using Microsoft.Extensions.Logging;
using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;
using SecretSanta.Infrastructure.AI;
using SecretSanta.Infrastructure.Telegram;
using Microsoft.Extensions.Configuration;
using System.Text;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Models;

namespace SecretSanta.Domain.Services;

public class ConversationOrchestrator : IConversationOrchestrator
{
	readonly ITextGenerationService _textGeneration;
	readonly IPendingUserInputService _pending;
	readonly ITelegramSender _telegram;
	readonly IParticipantService _participantService;
	readonly IWizardRegistry _registry;
	readonly ILogger<ConversationOrchestrator> _logger;
	readonly IConversationRepository _conversationRepo;
	private readonly TimeSpan _conversationTimeout;

	public ConversationOrchestrator(ITextGenerationService textGeneration, IPendingUserInputService pending, ITelegramSender telegram, IParticipantService participantService, IWizardRegistry registry, IConversationRepository conversationRepo, IConfiguration configuration, ILogger<ConversationOrchestrator> logger)
	{
		_textGeneration = textGeneration;
		_pending = pending;
		_telegram = telegram;
		_participantService = participantService;
		_registry = registry;
		_conversationRepo = conversationRepo;
		_logger = logger;

		var minutes = int.TryParse(configuration["Conversation:TimeoutMinutes"], out var m) ? m : 60 * 24 * 7; // default 7 days
		_conversationTimeout = TimeSpan.FromMinutes(minutes);
	}

	public async Task StartConversationAsync(string eventId, string santaTelegramUserId, string recipientTelegramUserId, string initiatorTelegramUserId, CancellationToken cancellationToken = default)
	{
		// permissions: ensure santa and recipient are registered participants
		var santa = await _participantService.GetByTelegramUserIdAsync(eventId, santaTelegramUserId, cancellationToken);
		var recipient = await _participantService.GetByTelegramUserIdAsync(eventId, recipientTelegramUserId, cancellationToken);
		if (santa == null || recipient == null)
		{
			throw new InvalidOperationException("Either santa or recipient is not registered for this event.");
		}

		if (santaTelegramUserId == recipientTelegramUserId)
		{
			throw new InvalidOperationException("Santa and recipient cannot be the same user.");
		}

		// create persistent conversation
		var conv = new Conversation { EventId = eventId, SantaTelegramUserId = santaTelegramUserId, RecipientTelegramUserId = recipientTelegramUserId, Status = "Active" };
		await _conversationRepo.CreateConversationAsync(conv, cancellationToken);

		// Store conversation state for both participants so either side can reply.
		var cs = new ConversationState { WizardName = "santa_dialog", StepIndex = 0 };
		cs.Data["conversationId"] = conv.Id;
		cs.Data["santa"] = santaTelegramUserId;
		cs.Data["recipient"] = recipientTelegramUserId;
		var santaState = new ConversationState { WizardName = "santa_dialog", StepIndex = 0 };
		santaState.Data["conversationId"] = conv.Id;
		santaState.Data["santa"] = santaTelegramUserId;
		santaState.Data["recipient"] = recipientTelegramUserId;
		santaState.Data["role"] = "santa";
		_pending.StartConversation(santaTelegramUserId, santaState);
		cs.Data["role"] = "recipient";
		_pending.StartConversation(recipientTelegramUserId, cs);

		var progressMessage = "Пробую зв’язатися з AI-помічником Санти. Це може зайняти кілька секунд.";
		await _telegram.SendMessageAsync(initiatorTelegramUserId, progressMessage);

		// Prepare the initial prompt when the text-generation service is available.
		var prompt = $"System: Ти — анонімний помічник Таємного Санти. Відповідай лише українською. Склади коротке анонімне запитання про подарунок. Не розкривай особу Санти. Вішліст отримувача: {recipient.WishlistText ?? "не заповнений"}.";
		string? generated = null;
		try
		{
			generated = await _textGeneration.GenerateResponseAsync(prompt, cancellationToken, async token =>
			{
				const string retryMessage = "Продовжую стукати до AI-помічника Санти — поки ніхто не відповідає.";
				await _telegram.SendMessageAsync(initiatorTelegramUserId, retryMessage);
			});
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			await CloseConversationAfterStartupCancellationAsync(conv, santaTelegramUserId, recipientTelegramUserId);
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Groq is unavailable; starting this conversation in direct anonymous mode.");
		}

		var aiEnabled = !string.IsNullOrWhiteSpace(generated);
		cs.Data["aiEnabled"] = aiEnabled.ToString();
		santaState.Data["aiEnabled"] = aiEnabled.ToString();
		_pending.StartConversation(recipientTelegramUserId, cs);

		if (aiEnabled)
		{
			var msg = new ConversationMessage { EventId = eventId, ConversationId = conv.Id, SenderTelegramUserId = "ai", SenderDisplayName = "SecretSantaBot", Text = generated!, MessageType = "AiGeneratedMessage" };
			await _conversationRepo.AddMessageAsync(msg, cancellationToken);
		}

		try
		{
			if (aiEnabled)
			{
				await _telegram.SendMessageAsync(recipientTelegramUserId, generated!);
			}
			else
			{
				var endKeyboard = BuildEndConversationKeyboard();
				await _telegram.SendMessageAsync(recipientTelegramUserId, "AI-помічник Санти зараз зайнятий. Можете продовжити анонімне спілкування напряму — надішліть повідомлення.", endKeyboard);
				await _telegram.SendMessageAsync(santaTelegramUserId, "AI-помічник Санти зараз зайнятий. Можете продовжити анонімне спілкування напряму — ваші повідомлення будуть передані отримувачу.", endKeyboard);
			}
			if (aiEnabled)
			{
				await _telegram.SendMessageAsync(santaTelegramUserId, "AI-помічник на зв’язку. Можете писати повідомлення — спілкування буде анонімним.");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send initial message to recipient");
			throw;
		}
	}

	private async Task CloseConversationAfterStartupCancellationAsync(Conversation conversation, string santaTelegramUserId, string recipientTelegramUserId)
	{
		conversation.Status = "Closed";
		await _conversationRepo.UpdateConversationAsync(conversation, CancellationToken.None);
		_pending.EndConversation(santaTelegramUserId);
		_pending.EndConversation(recipientTelegramUserId);
	}

	public async Task<string?> HandleIncomingMessageAsync(string eventId, string senderTelegramUserId, string text, CancellationToken cancellationToken = default)
	{
		// Check active conversation state for either participant.
		if (!_pending.TryGetConversation(senderTelegramUserId, out var state) || state == null)
		{
			// Recover a conversation initiated by the santa, if available.
			var conv = await _conversationRepo.GetActiveForParticipantAsync(eventId, senderTelegramUserId, cancellationToken);
			if (conv == null) return null;

			// if persistent conv exists but no transient state, recreate transient state
			state = new ConversationState { WizardName = "santa_dialog", StepIndex = 0 };
			state.Data["conversationId"] = conv.Id;
			state.Data["santa"] = conv.SantaTelegramUserId;
			state.Data["recipient"] = conv.RecipientTelegramUserId;
			_pending.StartConversation(senderTelegramUserId, state);
		}

		// load conversation id and check timeout
		state.Data.TryGetValue("conversationId", out var convId);
		if (string.IsNullOrEmpty(convId)) return null;

		state.Data.TryGetValue("recipient", out var recipientId);
		state.Data.TryGetValue("santa", out var santaId);
		var aiEnabled = !state.Data.TryGetValue("aiEnabled", out var aiEnabledValue) || !bool.TryParse(aiEnabledValue, out var parsedAiEnabled) || parsedAiEnabled;
		var convEntity = await _conversationRepo.GetActiveForParticipantAsync(eventId, senderTelegramUserId, cancellationToken);
		if (convEntity == null) return null;

		if (DateTime.UtcNow - convEntity.UpdatedAtUtc > _conversationTimeout)
		{
			// expire
			convEntity.Status = "Closed";
			await _conversationRepo.UpdateConversationAsync(convEntity, cancellationToken);
			_pending.EndConversation(senderTelegramUserId);
			return "Ця сесія закінчилася через тривалу неактивність.";
		}

		// persist incoming user message
		var userMsg = new ConversationMessage { EventId = eventId, ConversationId = convId, SenderTelegramUserId = senderTelegramUserId, Text = text, MessageType = "UserMessage" };
		await _conversationRepo.AddMessageAsync(userMsg, cancellationToken);
		var otherParticipantId = senderTelegramUserId == convEntity.SantaTelegramUserId ? convEntity.RecipientTelegramUserId : convEntity.SantaTelegramUserId;
		var anonymousSenderLabel = senderTelegramUserId == convEntity.SantaTelegramUserId ? "Санти" : "Отримувача";
		if (!aiEnabled)
		{
			await _telegram.SendMessageAsync(otherParticipantId, $"Анонімне повідомлення від {anonymousSenderLabel}:\n{text}");
			convEntity.UpdatedAtUtc = DateTime.UtcNow;
			await _conversationRepo.UpdateConversationAsync(convEntity, cancellationToken);
			_pending.UpdateConversation(senderTelegramUserId, state);
			return "Повідомлення доставлено.";
		}

		// Build prompt using conversation history and the recipient's wishlist.
		var history = state.Data.GetValueOrDefault("history", string.Empty);
		history += $"Учасник: {text}\n";
		state.Data["history"] = history;
		var recipient = await _participantService.GetByTelegramUserIdAsync(eventId, recipientId ?? convEntity.RecipientTelegramUserId, cancellationToken);

		var promptBuilder = new StringBuilder();
		promptBuilder.AppendLine("System: Ти — анонімний помічник Таємного Санти. Відповідай виключно українською, зберігай анонімність і будь привітним.");
		promptBuilder.AppendLine($"Вішліст отримувача: {recipient?.WishlistText ?? "не заповнений"}");
		promptBuilder.AppendLine("Історія комунікації:");
		promptBuilder.AppendLine(history);
		promptBuilder.AppendLine("Постав доречне уточнювальне запитання або коротко підсумуй відповідь українською.");

		string next;
		try
		{
			next = await _textGeneration.GenerateResponseAsync(promptBuilder.ToString(), cancellationToken);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Groq is unavailable; switching this conversation to direct anonymous mode.");
			state.Data["aiEnabled"] = bool.FalseString;
			if (_pending.TryGetConversation(otherParticipantId, out var otherState) && otherState != null)
			{
				otherState.Data["aiEnabled"] = bool.FalseString;
				_pending.UpdateConversation(otherParticipantId, otherState);
			}
			await _telegram.SendMessageAsync(otherParticipantId, $"Анонімне повідомлення від {anonymousSenderLabel}:\n{text}");
			convEntity.UpdatedAtUtc = DateTime.UtcNow;
			await _conversationRepo.UpdateConversationAsync(convEntity, cancellationToken);
			_pending.UpdateConversation(senderTelegramUserId, state);
			return "AI зараз недоступний. Повідомлення доставлено.";
		}

		// persist AI message
		var aiMsg = new ConversationMessage { EventId = eventId, ConversationId = convId, SenderTelegramUserId = "ai", SenderDisplayName = "SecretSantaBot", Text = next, MessageType = "AiGeneratedMessage" };
		await _conversationRepo.AddMessageAsync(aiMsg, cancellationToken);

		// Send AI's Ukrainian response to the current participant and forward the anonymous exchange to the other side.
		try
		{
			await _telegram.SendMessageAsync(otherParticipantId, $"Анонімне повідомлення від {anonymousSenderLabel}:\n{text}\n\nУточнення помічника:\n{next}");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send follow-up to recipient");
		}

		// update persistent conversation last-updated
		convEntity.UpdatedAtUtc = DateTime.UtcNow;
		await _conversationRepo.UpdateConversationAsync(convEntity, cancellationToken);

		// update transient state
		_pending.UpdateConversation(senderTelegramUserId, state);

		return next;
	}

	public async Task EndConversationAsync(string eventId, string initiatorTelegramUserId, CancellationToken cancellationToken = default)
	{
		// attempt to find conversation where initiator is either santa or recipient
		var conv = await _conversationRepo.GetActiveForParticipantAsync(eventId, initiatorTelegramUserId, cancellationToken);
		if (conv == null) return;

		conv.Status = "Closed";
		await _conversationRepo.UpdateConversationAsync(conv, cancellationToken);
		_pending.EndConversation(conv.RecipientTelegramUserId);
		_pending.EndConversation(conv.SantaTelegramUserId);

		// notify both parties
		try
		{
		var nextActions = BuildPostConversationKeyboard();
		await _telegram.SendMessageAsync(conv.RecipientTelegramUserId, "Діалог завершено. Оберіть, що робити далі:", nextActions);
		await _telegram.SendMessageAsync(conv.SantaTelegramUserId, "Діалог завершено. Оберіть, що робити далі:", nextActions);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send cancellation messages");
		}
	}

	private static Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup BuildEndConversationKeyboard() => new(new[]
	{
		new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("Завершити діалог", "conversation:end") }
	});

	private static Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup BuildPostConversationKeyboard() => new(new[]
	{
		new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("Уточнити в отримувача", "chat:recipient") },
		new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("Написати Secret Santa", "chat:santa") }
	});
}

