using MediatR;
using SecretSanta.Domain.Abstractions;
using System.Net;
using System.Text;

namespace SecretSanta.Domain.Handlers.Commands.Profile;

public class ProfileHandler(IParticipantService _participantService) : IRequestHandler<ProfileRequest, ProfileResponse>
{
	public async Task<ProfileResponse> Handle(ProfileRequest request, CancellationToken cancellationToken)
	{
		var telegramUserId = request.Message?.From?.Id.ToString() ?? string.Empty;
		var eventId = request.EventId;

		if (string.IsNullOrEmpty(telegramUserId))
		{
			return new ProfileResponse { StatusCode = HttpStatusCode.BadRequest, Payload = new { text = "Missing user id" } };
		}

		var participant = await _participantService.GetByTelegramUserIdAsync(eventId, telegramUserId, cancellationToken);
		if (participant == null)
		{
			return new ProfileResponse
			{
				StatusCode = HttpStatusCode.OK,
				Payload = default,
				ResponseMessage = "Ви не зареєстровані. Використайте /start щоб зареєструватись."
			};
		}

		var sb = new StringBuilder();
		sb.AppendLine($"Ім'я: @{participant.DisplayName}");
		sb.AppendLine($"Статус: {participant.RegistrationStatus}");
		sb.AppendLine($"Telegram Id: {participant.TelegramUserId}");
		sb.AppendLine($"Event: {participant.EventId}");

		if (!string.IsNullOrWhiteSpace(participant.WishlistText)) sb.AppendLine($"Wishlist: {participant.WishlistText}");
		if (!string.IsNullOrWhiteSpace(participant.Hobbies)) sb.AppendLine($"Hobbies: {participant.Hobbies}");
		if (!string.IsNullOrWhiteSpace(participant.PreferredGiftCategories)) sb.AppendLine($"Preferred categories: {participant.PreferredGiftCategories}");
		if (!string.IsNullOrWhiteSpace(participant.AvoidCategories)) sb.AppendLine($"Avoid categories: {participant.AvoidCategories}");
		if (!string.IsNullOrWhiteSpace(participant.PreferredColors)) sb.AppendLine($"Preferred colors: {participant.PreferredColors}");
		if (!string.IsNullOrWhiteSpace(participant.OtherPreferences)) sb.AppendLine($"Other: {participant.OtherPreferences}");

		sb.AppendLine();
		sb.AppendLine("Щоб додати або змінити ваші побажання щодо подарунку, скористайтесь командою /wishlist. Після команди бот попросить ввести текст побажань та збереже її у вашому профілі.");

		return new ProfileResponse
		{
			StatusCode = HttpStatusCode.OK,
			Payload = default,
			ResponseMessage = sb.ToString()
		};
	}
}
