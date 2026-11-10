using MediatR;
using SecretSanta.Domain.Enums;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.DAL.Abstractions;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Models;
using SecretSanta.Infrastructure.Telegram;
using Telegram.Bot.Types.ReplyMarkups;

namespace SecretSanta.Domain.Handlers.Commands.Participant;

public class MarkReadyHandler : IRequestHandler<MarkReadyRequest, CommandResponse>
{
	private readonly IParticipantRepository _participantRepo;
	private readonly IEventRepository _eventRepo;
	private readonly IPendingUserInputService _pending;
	private readonly ITelegramSender _telegramSender;

	public MarkReadyHandler(IParticipantRepository participantRepo, IEventRepository eventRepo, IPendingUserInputService pending, ITelegramSender telegramSender)
	{
		_participantRepo = participantRepo;
		_eventRepo = eventRepo;
		_pending = pending;
		_telegramSender = telegramSender;
	}

	public async Task<CommandResponse> Handle(MarkReadyRequest request, CancellationToken cancellationToken)
	{
		var ev = await _eventRepo.GetByEventIdAsync(request.EventId, cancellationToken);
		if (ev == null) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Event not found." };

		var participant = await _participantRepo.GetByTelegramUserIdAsync(request.EventId, request.TelegramUserId.ToString(), cancellationToken);
		if (participant == null) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Participant not found." };

		var status = string.IsNullOrWhiteSpace(request.GiftStatus) ? GiftStatusValue.Ready : request.GiftStatus.Trim();
		var normalized = status.Equals(GiftStatusValue.InProgress, StringComparison.OrdinalIgnoreCase) ? GiftStatusValue.InProgress : GiftStatusValue.Ready;

		participant.RegistrationStatus = normalized;
		participant.GiftStatus = normalized;
		participant.UpdatedAtUtc = DateTime.UtcNow;
		if (normalized == GiftStatusValue.Ready)
		{
			participant.GiftReadyAtUtc ??= DateTime.UtcNow;
		}
		await _participantRepo.UpdateAsync(participant, cancellationToken);

		var all = (await _participantRepo.GetAllByEventAsync(request.EventId, cancellationToken)).ToList();
		var readyCount = all.Count(p => string.Equals(p.GiftStatus, GiftStatusValue.Ready, StringComparison.OrdinalIgnoreCase));
		ev.GiftStatusSummary = $"{readyCount}/{all.Count} готові";
		ev.UpdatedAtUtc = DateTime.UtcNow;

		if (all.Count > 0 && all.All(p => string.Equals(p.GiftStatus, GiftStatusValue.Ready, StringComparison.OrdinalIgnoreCase)))
		{
			var notifyParticipants = ev.State != EventWorkflowState.AwaitingDelivery;
			ev.State = EventWorkflowState.AwaitingDelivery;
			ev.DeliveryRequestedAtUtc ??= DateTime.UtcNow;
			await _eventRepo.UpdateAsync(ev, cancellationToken);
			if (notifyParticipants)
			{
				var organizerId = ev.OrganizerTelegramId.ToString();
				_pending.StartConversation(organizerId, new ConversationState { WizardName = "delivery_info" });
				await _telegramSender.SendMessageAsync(organizerId, "Надішліть одним повідомленням місце й час вручення.");
				foreach (var eventParticipant in all)
				{
					await _telegramSender.SendMessageAsync(eventParticipant.TelegramUserId, "Усі учасники позначили подарунки готовими. Організатор має вказати місце й час вручення.");
				}
			}
			return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NoContent };
		}

		ev.State = EventWorkflowState.Preparing;
		await _eventRepo.UpdateAsync(ev, cancellationToken);
		return new CommandResponse
		{
			StatusCode = System.Net.HttpStatusCode.OK,
			ResponseMessage = normalized == GiftStatusValue.Ready ? "Ваш подарунок позначений як готовий." : "Ваш статус оновлено: ще уточнюю.",
			ReplyMarkup = BuildGiftStatusKeyboard(normalized)
		};
	}

	private static InlineKeyboardMarkup BuildGiftStatusKeyboard(string currentStatus) => currentStatus == GiftStatusValue.Ready
		? new(new[] { new[] { InlineKeyboardButton.WithCallbackData("Ще хочу дещо уточнити", "gift:progress") } })
		: new(new[]
		{
			new[] { InlineKeyboardButton.WithCallbackData("Подарунок вже готовий", "gift:ready") },
			new[] { InlineKeyboardButton.WithCallbackData("Ще хочу дещо уточнити", "gift:progress") },
			new[] { InlineKeyboardButton.WithCallbackData("Уточнити в отримувача", "chat:recipient") },
			new[] { InlineKeyboardButton.WithCallbackData("Написати Secret Santa", "chat:santa") }
		});
}
