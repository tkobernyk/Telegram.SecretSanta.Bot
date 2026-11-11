using SecretSanta.Domain.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SecretSanta.Domain.Helpers;

public static class EventParticipantKeyboard
{
	public static InlineKeyboardMarkup? Create(string eventState)
	{
		if (string.Equals(eventState, EventWorkflowState.Registration, StringComparison.OrdinalIgnoreCase))
		{
			return ProfileWishlistKeyboard.Create();
		}

		if (eventState is EventWorkflowState.Draft or EventWorkflowState.Preparing)
		{
			return new InlineKeyboardMarkup(new[]
			{
				new[] { InlineKeyboardButton.WithCallbackData("💬Уточнити в отримувача", "chat:recipient") },
				new[] { InlineKeyboardButton.WithCallbackData("🎅Написати Секретному Миколаю", "chat:santa") },
				new[] { InlineKeyboardButton.WithCallbackData("🎁Подарунок готовий", "gift:ready") }
			});
		}

		return null;
	}
}
