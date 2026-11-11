using MediatR;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Models;
using SecretSanta.Domain.Mappings;
using SecretSanta.Domain.Helpers;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.DAL.Abstractions.Repositories;
using System.Net;

namespace SecretSanta.Domain.Handlers.Commands.Wishlist;

public class WishlistHandler(IParticipantRepository _participantRepository, IWizardRegistry _registry) : IRequestHandler<WishlistRequest, WishlistResponse>
{
	public async Task<WishlistResponse> Handle(WishlistRequest request, CancellationToken cancellationToken)
	{
		var telegramUserId = request.Message?.From?.Id.ToString() ?? string.Empty;
		var eventId = request.EventId;
		var text = request.Message?.Text?.Trim() ?? string.Empty;

		// start from inline callback action
		if (string.Equals(text, "wishlist:edit", StringComparison.OrdinalIgnoreCase))
		{
			var existingEntity = await _participantRepository.GetByTelegramUserIdAsync(eventId, telegramUserId, cancellationToken);
			if (existingEntity == null)
			{
				return new WishlistResponse { StatusCode = HttpStatusCode.OK, ResponseMessage = "Ви не зареєстровані. Використайте /start щоб зареєструватись." };
			}

			var steps = _registry.GetSteps("wishlist");
			var convState = new ConversationState { WizardName = "wishlist", StepIndex = 0 };
			InteractionStateStore.StartConversation(telegramUserId, convState);
			return new WishlistResponse { StatusCode = HttpStatusCode.OK, ResponseMessage = steps[0].Prompt };
		}

		// check conversation
		if (InteractionStateStore.TryGetConversation(telegramUserId, out var state) && state != null && string.Equals(state.WizardName, "wishlist", System.StringComparison.OrdinalIgnoreCase))
		{
			var steps = _registry.GetSteps("wishlist");
			// save response to current field
			if (state.StepIndex < 0 || state.StepIndex >= steps.Count)
			{
				state.StepIndex = 0;
			}
			var current = steps[state.StepIndex];
			state.Data[current.FieldName] = text ?? string.Empty;
			state.StepIndex++;

			if (state.StepIndex < steps.Count)
			{
				InteractionStateStore.UpdateConversation(telegramUserId, state);
				return new WishlistResponse { StatusCode = HttpStatusCode.OK, ResponseMessage = steps[state.StepIndex].Prompt };
			}

			// finished: persist
			InteractionStateStore.EndConversation(telegramUserId);
			var participantEntity = await _participantRepository.GetByTelegramUserIdAsync(eventId, telegramUserId, cancellationToken);
			if (participantEntity == null)
			{
				return new WishlistResponse { StatusCode = HttpStatusCode.OK, ResponseMessage = "Ви не зареєстровані. Використайте /start щоб зареструватись." };
			}

			var participant = ParticipantMapper.Map(participantEntity);
			participant.WishlistText = state.Data.GetValueOrDefault("WishlistText", participant.WishlistText ?? string.Empty);
			participant.Hobbies = state.Data.GetValueOrDefault("Hobbies", participant.Hobbies ?? string.Empty);
			participant.PreferredGiftCategories = state.Data.GetValueOrDefault("PreferredGiftCategories", participant.PreferredGiftCategories ?? string.Empty);
			participant.AvoidCategories = state.Data.GetValueOrDefault("AvoidCategories", participant.AvoidCategories ?? string.Empty);
			participant.PreferredColors = state.Data.GetValueOrDefault("PreferredColors", participant.PreferredColors ?? string.Empty);
			participant.OtherPreferences = state.Data.GetValueOrDefault("OtherPreferences", participant.OtherPreferences ?? string.Empty);

			await _participantRepository.UpdateAsync(ParticipantMapper.Map(participant), cancellationToken);
			return new WishlistResponse
			{
				StatusCode = HttpStatusCode.OK,
				ResponseMessage = "Усі ваші побажання збережені.",
				ReplyMarkup = ProfileWishlistKeyboard.Create()
			};
		}

		return new WishlistResponse { StatusCode = HttpStatusCode.OK };
	}
}
