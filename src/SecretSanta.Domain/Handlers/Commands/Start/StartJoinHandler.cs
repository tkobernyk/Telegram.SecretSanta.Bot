using MediatR;
using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Models;
using SecretSanta.Domain.Mappings;
using SecretSanta.Domain.Helpers;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.DAL.Abstractions.Repositories;

namespace SecretSanta.Domain.Handlers.Commands.Start;

public class StartJoinHandler(IParticipantRepository participantRepository, IEventRepository eventRepository, IUserEventSessionRepository userEventSessionRepository) : IRequestHandler<StartJoinRequest, StartResponse>
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
			InteractionStateStore.StartConversation(telegramUserId, new ConversationState { WizardName = "start_event", StepIndex = 1 });
			return new StartResponse
			{
				StatusCode = System.Net.HttpStatusCode.OK,
				ResponseMessage = "Введіть ключ доступу до Секретного Миколайка, який вам дав друг."
			};
		}

		var eventEntity = await eventRepository.GetByJoinCodeAsync(joinCode, cancellationToken);
		if (eventEntity == null)
		{
			InteractionStateStore.StartConversation(telegramUserId, new ConversationState { WizardName = "start_event", StepIndex = 1 });
			return new StartResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Не знайдено Секретного Миколайка за таким ключем. Перевірте код і спробуйте ще раз." };
		}

		var participantEntity = await participantRepository.GetByTelegramUserIdAsync(eventEntity.EventId, telegramUserId, cancellationToken);
		if (participantEntity != null)
		{
			InteractionStateStore.SetActiveEventId(telegramUserId, eventEntity.EventId);
			await userEventSessionRepository.UpsertAsync(new UserEventSession { TelegramUserId = telegramUserId, EventId = eventEntity.EventId }, cancellationToken);
			InteractionStateStore.EndConversation(telegramUserId);
			return new StartResponse
			{
				StatusCode = System.Net.HttpStatusCode.OK,
				ResponseMessage = "Ви вже приєднані до цієї події Секретного Миколайка.",
				ReplyMarkup = EventParticipantKeyboard.Create(eventEntity.State)
			};
		}

		var displayName = $"{request.Message?.From?.FirstName} {request.Message?.From?.LastName} (@{request.Message?.From?.Username ?? $"user-{telegramUserId}"})";
		var created = await RegisterParticipantAsync(eventEntity.EventId, telegramUserId, displayName, cancellationToken);
		InteractionStateStore.SetActiveEventId(telegramUserId, eventEntity.EventId);
		await userEventSessionRepository.UpsertAsync(new UserEventSession { TelegramUserId = telegramUserId, EventId = eventEntity.EventId }, cancellationToken);
		InteractionStateStore.EndConversation(telegramUserId);
		return new StartResponse
		{
			StatusCode = System.Net.HttpStatusCode.Created,
			Payload = new { eventId = eventEntity.EventId, joinCode = eventEntity.JoinCode },
			ResponseMessage = $"Ви успішно приєдналися до події Секретного Миколайка. Ключ: {eventEntity.JoinCode}\nЗаповніть ваш профіль або побажання кнопками нижче.",
			ReplyMarkup = ProfileWishlistKeyboard.Create()
		};
	}

	private async Task<StartResponse> RegisterParticipantAsync(string eventId, string telegramUserId, string displayName, CancellationToken cancellationToken)
	{
		var existingEntity = await participantRepository.GetByTelegramUserIdAsync(eventId, telegramUserId, cancellationToken);
		if (existingEntity != null)
		{
			var existing = ParticipantMapper.Map(existingEntity);
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

		await participantRepository.CreateAsync(ParticipantMapper.Map(participant), cancellationToken);
		return new StartResponse
		{
			StatusCode = System.Net.HttpStatusCode.Created,
			Payload = default,
			ResponseMessage = $"Дякую, {participant.DisplayName}! Ви успішно зареєстровані. Перегляньте профіль або заповніть побажання кнопками нижче.",
			ReplyMarkup = ProfileWishlistKeyboard.Create()
		};
	}
}
