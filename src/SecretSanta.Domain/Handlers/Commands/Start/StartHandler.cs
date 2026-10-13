using MediatR;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Models;
using SecretSanta.Infrastructure.Telegram;

namespace SecretSanta.Domain.Handlers.Commands.Start;

public class StartHandler(IParticipantService _participantService) : IRequestHandler<StartRequest, StartResponse>
{
	public async Task<StartResponse> Handle(StartRequest request, CancellationToken cancellationToken)
	{
		var telegramUserId = request.Message?.From?.Id.ToString() ?? string.Empty;
		var eventId = request.EventId;

		if (string.IsNullOrEmpty(telegramUserId))
		{
			return new StartResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, Payload = new { text = "Missing user id" } };
		}

		var existing = await _participantService.GetByTelegramUserIdAsync(eventId, telegramUserId, cancellationToken);
		if (existing != null)
		{
			return new StartResponse
			{
				StatusCode = System.Net.HttpStatusCode.OK,
				Payload = default,
				ResponseMessage = $"Ви вже зареєстровані, {existing.DisplayName}. Використайте /profile щоб переглянути анкету."
			};
		}

		var displayName = request.Message?.From?.FirstName ?? request.Message?.From?.Username ?? ("user-" + telegramUserId);

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

		await _participantService.CreateAsync(participant, cancellationToken);

		return new StartResponse {
			StatusCode = System.Net.HttpStatusCode.Created,
			Payload = default,
			ResponseMessage = $"Дякую, {participant.DisplayName}! Ви успішно зареєстровані. Використайте /profile щоб переглянути анкету."
		};
	}
}
