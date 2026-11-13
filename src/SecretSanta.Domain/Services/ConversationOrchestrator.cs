using Microsoft.Extensions.Logging;
using SecretSanta.DAL.Entities;
using SecretSanta.Infrastructure.AI;
using SecretSanta.Infrastructure.Telegram;
using Microsoft.Extensions.Configuration;
using System.Text;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Models;
using SecretSanta.Domain.Mappings;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.DAL.Abstractions.Repositories;

namespace SecretSanta.Domain.Services;

public class ConversationOrchestrator : IConversationOrchestrator
{
	readonly ITextGenerationService _textGeneration;
	readonly ITelegramSender _telegram;
	readonly IParticipantRepository _participantRepository;
	readonly IWizardRegistry _registry;
	readonly ILogger<ConversationOrchestrator> _logger;
	readonly IConversationRepository _conversationRepo;
	private readonly TimeSpan _conversationTimeout;

	public ConversationOrchestrator(ITextGenerationService textGeneration, ITelegramSender telegram, IParticipantRepository participantRepository, IWizardRegistry registry, IConversationRepository conversationRepo, IConfiguration configuration, ILogger<ConversationOrchestrator> logger)
	{
		_textGeneration = textGeneration;
		_telegram = telegram;
		_participantRepository = participantRepository;
		_registry = registry;
		_conversationRepo = conversationRepo;
		_logger = logger;

		var minutes = int.TryParse(configuration["Conversation:TimeoutMinutes"], out var m) ? m : 60 * 24 * 7; // default 7 days
		_conversationTimeout = TimeSpan.FromMinutes(minutes);
	}

	public async Task StartConversationAsync(string eventId, string santaTelegramUserId, string recipientTelegramUserId, string initiatorTelegramUserId, CancellationToken cancellationToken = default)
	{
		var santaEntity = await _participantRepository.GetByTelegramUserIdAsync(eventId, santaTelegramUserId, cancellationToken);
		var recipientEntity = await _participantRepository.GetByTelegramUserIdAsync(eventId, recipientTelegramUserId, cancellationToken);
		if (santaEntity == null || recipientEntity == null)
		{
			throw new InvalidOperationException("Either santa or recipient is not registered for this event.");
		}

		if (santaTelegramUserId == recipientTelegramUserId)
		{
			throw new InvalidOperationException("Santa and recipient cannot be the same user.");
		}

		if (!string.Equals(initiatorTelegramUserId, santaTelegramUserId, StringComparison.Ordinal)
			&& !string.Equals(initiatorTelegramUserId, recipientTelegramUserId, StringComparison.Ordinal))
		{
			throw new InvalidOperationException("Initiator must be either Миколайко or Отримувач.");
		}

		var conversation = new Conversation
		{
			EventId = eventId,
			SantaTelegramUserId = santaTelegramUserId,
			RecipientTelegramUserId = recipientTelegramUserId,
			Status = "Active"
		};
		await _conversationRepo.CreateConversationAsync(conversation, cancellationToken);

		var initiatorIsMykolayko = string.Equals(initiatorTelegramUserId, santaTelegramUserId, StringComparison.Ordinal);
		var initiatorRoleUk = initiatorIsMykolayko ? "Миколайко" : "Отримувач";
		var partnerRoleUk = initiatorIsMykolayko ? "Отримувач" : "Миколайко";
		var partnerTelegramUserId = initiatorIsMykolayko ? recipientTelegramUserId : santaTelegramUserId;

		var initiatorState = new ConversationState { WizardName = "santa_dialog", StepIndex = 0 };
		initiatorState.Data["conversationId"] = conversation.Id;
		initiatorState.Data["santa"] = santaTelegramUserId;
		initiatorState.Data["recipient"] = recipientTelegramUserId;
		initiatorState.Data["initiator"] = initiatorTelegramUserId;
		initiatorState.Data["counterparty"] = partnerTelegramUserId;
		initiatorState.Data["roleUk"] = initiatorRoleUk;
		initiatorState.Data["counterpartyRoleUk"] = partnerRoleUk;
		InteractionStateStore.StartConversation(initiatorTelegramUserId, initiatorState);

		var counterpartyState = new ConversationState { WizardName = "santa_dialog", StepIndex = 0 };
		counterpartyState.Data["conversationId"] = conversation.Id;
		counterpartyState.Data["santa"] = santaTelegramUserId;
		counterpartyState.Data["recipient"] = recipientTelegramUserId;
		counterpartyState.Data["initiator"] = initiatorTelegramUserId;
		counterpartyState.Data["counterparty"] = initiatorTelegramUserId;
		counterpartyState.Data["roleUk"] = partnerRoleUk;
		counterpartyState.Data["counterpartyRoleUk"] = initiatorRoleUk;
		InteractionStateStore.StartConversation(partnerTelegramUserId, counterpartyState);

		await _telegram.SendMessageAsync(initiatorTelegramUserId, $"Ви в ролі «{initiatorRoleUk}». Що ви хочете запитати?", BuildConversationEndKeyboard());
	}
	private async Task CloseConversationAfterStartupCancellationAsync(Conversation conversation, string santaTelegramUserId, string recipientTelegramUserId)
	{
		conversation.Status = "Closed";
		await _conversationRepo.UpdateConversationAsync(conversation, CancellationToken.None);
		InteractionStateStore.EndConversation(santaTelegramUserId);
		InteractionStateStore.EndConversation(recipientTelegramUserId);
	}

	public async Task<string?> HandleIncomingMessageAsync(string eventId, string senderTelegramUserId, string text, CancellationToken cancellationToken = default)
	{
		if (!InteractionStateStore.TryGetConversation(senderTelegramUserId, out var state) || state == null || !string.Equals(state.WizardName, "santa_dialog", StringComparison.OrdinalIgnoreCase))
		{
			state = new ConversationState { WizardName = "santa_dialog", StepIndex = 0 };
		}

		var convEntity = await _conversationRepo.GetActiveForParticipantAsync(eventId, senderTelegramUserId, cancellationToken);
		if (convEntity == null)
		{
			InteractionStateStore.EndConversation(senderTelegramUserId);
			return null;
		}

		state.Data["conversationId"] = convEntity.Id;
		state.Data["santa"] = convEntity.SantaTelegramUserId;
		state.Data["recipient"] = convEntity.RecipientTelegramUserId;
		state.Data.TryGetValue("conversationId", out var conversationId);

		if (DateTime.UtcNow - convEntity.UpdatedAtUtc > _conversationTimeout)
		{
			convEntity.Status = "Closed";
			await _conversationRepo.UpdateConversationAsync(convEntity, cancellationToken);
			InteractionStateStore.EndConversation(senderTelegramUserId);
			return "Ця сесія закінчилася через тривалу неактивність.";
		}

		state.Data.TryGetValue("counterparty", out var counterpartyId);
		if (string.IsNullOrWhiteSpace(counterpartyId))
		{
			counterpartyId = string.Equals(senderTelegramUserId, convEntity.SantaTelegramUserId, StringComparison.Ordinal)
				? convEntity.RecipientTelegramUserId
				: convEntity.SantaTelegramUserId;
			state.Data["counterparty"] = counterpartyId;
		}

		state.Data.TryGetValue("roleUk", out var senderRoleUk);
		senderRoleUk ??= string.Equals(senderTelegramUserId, convEntity.SantaTelegramUserId, StringComparison.Ordinal)
			? "Миколайко"
			: "Отримувач";
		state.Data["roleUk"] = senderRoleUk;

		var userMsg = new ConversationMessage
		{
			EventId = eventId,
			ConversationId = conversationId,
			SenderTelegramUserId = senderTelegramUserId,
			Text = text,
			MessageType = "UserMessage"
		};
		await _conversationRepo.AddMessageAsync(userMsg, cancellationToken);

		convEntity.UpdatedAtUtc = DateTime.UtcNow;
		await _conversationRepo.UpdateConversationAsync(convEntity, cancellationToken);

		var prefixedMessage = $"{senderRoleUk}: {text}";
		await _telegram.SendMessageAsync(counterpartyId, prefixedMessage, BuildConversationEndKeyboard());
		await _telegram.SendMessageAsync(senderTelegramUserId, "Повідомлення надіслано.", BuildConversationEndKeyboard());

		InteractionStateStore.UpdateConversation(senderTelegramUserId, state);
		if (InteractionStateStore.TryGetConversation(counterpartyId, out var counterpartyState) && counterpartyState != null)
		{
			InteractionStateStore.UpdateConversation(counterpartyId, counterpartyState);
		}

		return null;
	}

	public async Task EndConversationAsync(string eventId, string initiatorTelegramUserId, CancellationToken cancellationToken = default)
	{
		var conv = await _conversationRepo.GetActiveForParticipantAsync(eventId, initiatorTelegramUserId, cancellationToken);
		if (conv == null)
		{
			InteractionStateStore.EndConversation(initiatorTelegramUserId);
			return;
		}

		await EndConversationInternalAsync(conv, initiatorTelegramUserId, cancellationToken);
	}

	private async Task EndConversationInternalAsync(Conversation conv, string initiatorTelegramUserId, CancellationToken cancellationToken)
	{
		conv.Status = "Closed";
		await _conversationRepo.UpdateConversationAsync(conv, cancellationToken);
		InteractionStateStore.EndConversation(conv.RecipientTelegramUserId);
		InteractionStateStore.EndConversation(conv.SantaTelegramUserId);
		InteractionStateStore.EndConversation(initiatorTelegramUserId);

		var initiatorRoleUk = string.Equals(initiatorTelegramUserId, conv.SantaTelegramUserId, StringComparison.Ordinal)
			? "Миколайко"
			: "Отримувач";
		var otherParticipantId = string.Equals(initiatorTelegramUserId, conv.SantaTelegramUserId, StringComparison.Ordinal)
			? conv.RecipientTelegramUserId
			: conv.SantaTelegramUserId;

		try
		{
			var postConversationKeyboard = BuildPostConversationKeyboard();
			await _telegram.SendMessageAsync(initiatorTelegramUserId, "Діалог завершено.", postConversationKeyboard);
			await _telegram.SendMessageAsync(otherParticipantId, $"Діалог завершено. Роль «{initiatorRoleUk}» завершила анонімне спілкування.", postConversationKeyboard);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send conversation end notifications.");
		}
	}

	static Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup BuildConversationEndKeyboard() => new(
	[
		[Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("👋Завершити діалог", "conversation:end")]
	]);

	static Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup BuildPostConversationKeyboard() => new(new[]
	{
		[Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("💬Уточнити в отримувача", "chat:recipient")],
		[Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🎅Запитати в Миколайка", "chat:santa")],
		new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🎁Подарунок готовий", "gift:ready") }
	});
}

