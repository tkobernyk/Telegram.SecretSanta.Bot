using MediatR;
using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Enums;
using SecretSanta.Domain.Models;
using SecretSanta.Domain.Helpers;

namespace SecretSanta.Domain.Handlers.Commands.Start;

public class StartCreateHandler(IParticipantService participantService, IEventRepository eventRepository, IPendingUserInputService pending) : IRequestHandler<StartCreateRequest, StartResponse>
{
	public async Task<StartResponse> Handle(StartCreateRequest request, CancellationToken cancellationToken)
	{
		var telegramUserId = request.TelegramUserId;
		if (string.IsNullOrWhiteSpace(telegramUserId))
		{
			return new StartResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Missing telegram user id." };
		}

		var displayName = request.DisplayName ?? request.Message?.From?.FirstName ?? request.Message?.From?.Username ?? $"user-{telegramUserId}";
		var eventEntity = await CreateNewEventAsync(telegramUserId, displayName, cancellationToken);
		await pending.SetActiveEventIdAsync(telegramUserId, eventEntity.EventId, cancellationToken);
		pending.EndConversation(telegramUserId);
		return new StartResponse
		{
			StatusCode = System.Net.HttpStatusCode.Created,
			Payload = new { eventId = eventEntity.EventId, joinCode = eventEntity.JoinCode },
			ResponseMessage = $"Ви створили новий Secret Santa. Ключ доступу: {eventEntity.JoinCode}\nПоділіться ним із друзями.\nЗаповніть ваш профіль або побажання кнопками нижче.",
			ReplyMarkup = ProfileWishlistKeyboard.Create()
		};
	}

	private async Task<Event> CreateNewEventAsync(string telegramUserId, string displayName, CancellationToken cancellationToken)
	{
		var joinCode = await GenerateUniqueJoinCodeAsync(cancellationToken);
		var eventEntity = new Event
		{
			EventId = Guid.NewGuid().ToString("N"),
			Name = "Secret Santa",
			JoinCode = joinCode,
			OrganizerTelegramId = long.Parse(telegramUserId),
			CreatedByTelegramId = long.Parse(telegramUserId),
			State = EventWorkflowState.Registration,
			CreatedAtUtc = DateTime.UtcNow,
			UpdatedAtUtc = DateTime.UtcNow
		};

		await eventRepository.CreateAsync(eventEntity, cancellationToken);
		await RegisterParticipantAsync(eventEntity.EventId, telegramUserId, displayName, cancellationToken);
		return eventEntity;
	}

	private async Task<string> GenerateUniqueJoinCodeAsync(CancellationToken cancellationToken)
	{
		for (var i = 0; i < 20; i++)
		{
			var code = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
			if (await eventRepository.GetByJoinCodeAsync(code, cancellationToken) == null)
			{
				return code;
			}
		}

		throw new InvalidOperationException("Unable to generate unique join code for Secret Santa event.");
	}

	private async Task<StartResponse> RegisterParticipantAsync(string eventId, string telegramUserId, string displayName, CancellationToken cancellationToken)
	{
		var existing = await participantService.GetByTelegramUserIdAsync(eventId, telegramUserId, cancellationToken);
		if (existing != null)
		{
			return new StartResponse
			{
				StatusCode = System.Net.HttpStatusCode.OK,
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
			ResponseMessage = $"Дякую, {participant.DisplayName}! Ви успішно зареєстровані. Перегляньте профіль або заповніть побажання кнопками нижче.",
			ReplyMarkup = ProfileWishlistKeyboard.Create()
		};
	}

}
