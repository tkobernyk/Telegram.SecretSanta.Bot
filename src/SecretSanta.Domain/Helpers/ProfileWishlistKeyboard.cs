using Telegram.Bot.Types.ReplyMarkups;

namespace SecretSanta.Domain.Helpers;

public static class ProfileWishlistKeyboard
{
	public static InlineKeyboardMarkup Create() => new(new[]
	{
		new[] { InlineKeyboardButton.WithCallbackData("👤 Мій профіль", "profile:view") },
		new[] { InlineKeyboardButton.WithCallbackData("🎁 Мої побажання", "wishlist:edit") }
	});
}
