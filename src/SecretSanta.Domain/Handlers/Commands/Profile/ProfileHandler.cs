using MediatR;
using SecretSanta.DAL.Abstractions.Repositories;
using SecretSanta.Domain.Helpers;
using SecretSanta.Domain.Mappings;
using System.Net;
using System.Text;

namespace SecretSanta.Domain.Handlers.Commands.Profile;

public class ProfileHandler(IParticipantRepository _participantRepository) : IRequestHandler<ProfileRequest, ProfileResponse>
{
	public async Task<ProfileResponse> Handle(ProfileRequest request, CancellationToken cancellationToken)
	{
		var telegramUserId = request.Message?.From?.Id.ToString() ?? string.Empty;
		var eventId = request.EventId;

		if (string.IsNullOrEmpty(telegramUserId))
		{
			return new ProfileResponse { StatusCode = HttpStatusCode.BadRequest, Payload = new { text = "Missing user id" } };
		}

		var participantEntity = await _participantRepository.GetByTelegramUserIdAsync(eventId, telegramUserId, cancellationToken);
		if (participantEntity == null)
		{
			return new ProfileResponse
			{
				StatusCode = HttpStatusCode.OK,
				Payload = default,
				ResponseMessage = "Ви не зареєстровані. Використайте /start, щоб зареєструватись."
			};
		}

		var participant = ParticipantMapper.Map(participantEntity);
		var sb = new StringBuilder();
		sb.AppendLine($"Ім'я: {participant.DisplayName}");
		sb.AppendLine($"Статус: {participant.RegistrationStatus}");
		sb.AppendLine($"Ваш Telegram Id: {participant.TelegramUserId}");

		if (!string.IsNullOrWhiteSpace(participant.WishlistText)) sb.AppendLine($"Список бажань: {participant.WishlistText}");
		if (!string.IsNullOrWhiteSpace(participant.Hobbies)) sb.AppendLine($"Хобі: {participant.Hobbies}");
		if (!string.IsNullOrWhiteSpace(participant.PreferredGiftCategories)) sb.AppendLine($"Улюблені категорії подарунків: {participant.PreferredGiftCategories}");
		if (!string.IsNullOrWhiteSpace(participant.AvoidCategories)) sb.AppendLine($"Категорії, яких слід уникати: {participant.AvoidCategories}");
		if (!string.IsNullOrWhiteSpace(participant.PreferredColors)) sb.AppendLine($"Улюблені кольори: {participant.PreferredColors}");
		if (!string.IsNullOrWhiteSpace(participant.OtherPreferences)) sb.AppendLine($"Інші побажання: {participant.OtherPreferences}");

		sb.AppendLine();
		sb.AppendLine("Щоб додати або змінити побажання щодо подарунку, натисніть кнопку «Мої побажання».");

		return new ProfileResponse
		{
			StatusCode = HttpStatusCode.OK,
			Payload = default,
			ResponseMessage = sb.ToString(),
			ReplyMarkup = ProfileWishlistKeyboard.Create()
		};
	}
}
