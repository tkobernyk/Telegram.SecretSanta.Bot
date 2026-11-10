using MediatR;
using SecretSanta.DAL.Abstractions;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Models;
using SecretSanta.Domain.Helpers;

namespace SecretSanta.Domain.Handlers.Commands.Start;

public class StartJoinHandler(IParticipantService participantService, IEventRepository eventRepository, IPendingUserInputService pending) : IRequestHandler<StartJoinRequest, StartResponse>
{
	public async Task<StartResponse> Handle(StartJoinRequest request, CancellationToken cancellationToken)
	{
		var telegramUserId = request.TelegramUserId;
		var joinCode = (request.JoinCode ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(telegramUserId))
		{
			return new StartResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Missing telegram user id." };
		}

		if (string.IsNullOrWhiteSpace(joinCode))
		{
			pending.StartConversation(telegramUserId, new ConversationState { WizardName = "start_event", StepIndex = 1 });
			return new StartResponse
			{
				StatusCode = System.Net.HttpStatusCode.OK,
				ResponseMessage = "Введіть ключ доступу до Secret Santa, який вам дав друг."
			};
		}

		var eventEntity = await eventRepository.GetByJoinCodeAsync(joinCode, cancellationToken);
		if (eventEntity == null)
		{
			pending.StartConversation(telegramUserId, new ConversationState { WizardName = "start_event", StepIndex = 1 });
			return new StartResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Не знайдено Secret Santa за таким ключем. Перевірте код і спробуйте ще раз." };
		}

		var participant = await participantService.GetByTelegramUserIdAsync(eventEntity.EventId, telegramUserId, cancellationToken);
		if (participant != null)
		{
			await pending.SetActiveEventIdAsync(telegramUserId, eventEntity.EventId, cancellationToken);
			pending.EndConversation(telegramUserId);
			return new StartResponse
			{
				StatusCode = System.Net.HttpStatusCode.OK,
				ResponseMessage = "Ви вже приєднані до цього Secret Santa.",
				ReplyMarkup = EventParticipantKeyboard.Create(eventEntity.State)
			};
		}

		var displayName = request.DisplayName ?? request.Message?.From?.FirstName ?? request.Message?.From?.Username ?? $"user-{telegramUserId}";
		var created = await RegisterParticipantAsync(eventEntity.EventId, telegramUserId, displayName, cancellationToken);
		await pending.SetActiveEventIdAsync(telegramUserId, eventEntity.EventId, cancellationToken);
		pending.EndConversation(telegramUserId);
		return new StartResponse
		{
			StatusCode = System.Net.HttpStatusCode.Created,
			Payload = new { eventId = eventEntity.EventId, joinCode = eventEntity.JoinCode },
			ResponseMessage = $"Ви успішно приєдналися до Secret Santa. Ключ: {eventEntity.JoinCode}\nЗаповніть ваш профіль або побажання кнопками нижче.",
			ReplyMarkup = ProfileWishlistKeyboard.Create()
		};
	}

	private async Task<StartResponse> RegisterParticipantAsync(string eventId, string telegramUserId, string displayName, CancellationToken cancellationToken)
	{
		var existing = await participantService.GetByTelegramUserIdAsync(eventId, telegramUserId, cancellationToken);
		if (existing != null)
		{
			return new StartResponse
			{
				StatusCode = System.Net.HttpStatusCode.OK,
				Payload = default,
				ResponseMessage = $"Ви вже зареєстровані, {existing.DisplayName}. Перегляньте профіль або оновіть побажання кнопками нижче.",
				ReplyMarkup = ProfileWishlistKeyboard.Create()
			};
		}

		var participant = new ParticipantDto
		{
			EventId = eventId,
			TelegramUserId = telegramUserId,
			DisplayName = displayName,
			WishlistText = string.Empty,
			Hobbies = string.Empty,
			PreferredGiftCategories = string.Empty,
			AvoidCategories = string.Empty,
			PreferredColors = string.Empty,
			OtherPreferences = string.Empty,
			RegistrationStatus = "Registered",
			CreatedAtUtc = DateTime.UtcNow,
			UpdatedAtUtc = DateTime.UtcNow
		};

		await participantService.CreateAsync(participant, cancellationToken);
		return new StartResponse
		{
			StatusCode = System.Net.HttpStatusCode.Created,
			Payload = default,
			ResponseMessage = $"Дякую, {participant.DisplayName}! Ви успішно зареєстровані. Перегляньте профіль або заповніть побажання кнопками нижче.",
			ReplyMarkup = ProfileWishlistKeyboard.Create()
		};
	}
}
