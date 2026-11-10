using NUnit.Framework;
using SecretSanta.Domain.Helpers;
using Telegram.Bot.Types.ReplyMarkups;

namespace SecretSanta.Domain.Tests;

public class ProfileWishlistKeyboardTests
{
	[Test]
	public void Create_ExposesProfileAndWishlistActionsAsInlineButtons()
	{
		var keyboard = ProfileWishlistKeyboard.Create();
		var buttons = keyboard.InlineKeyboard.SelectMany(row => row).ToArray();

		Assert.That(buttons, Has.Length.EqualTo(2));
		Assert.That(buttons[0].CallbackData, Is.EqualTo("profile:view"));
		Assert.That(buttons[1].CallbackData, Is.EqualTo("wishlist:edit"));
	}
}
