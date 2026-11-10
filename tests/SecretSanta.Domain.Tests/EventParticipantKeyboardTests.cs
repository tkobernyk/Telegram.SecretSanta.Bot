using NUnit.Framework;
using SecretSanta.Domain.Enums;
using SecretSanta.Domain.Helpers;

namespace SecretSanta.Domain.Tests;

public class EventParticipantKeyboardTests
{
	[Test]
	public void Create_RegistrationState_ShowsProfileAndWishlist()
	{
		var keyboard = EventParticipantKeyboard.Create(EventWorkflowState.Registration);

		Assert.That(keyboard, Is.Not.Null);
		var callbackData = keyboard!.InlineKeyboard.SelectMany(row => row).Select(button => button.CallbackData).ToArray();
		Assert.That(callbackData, Is.EqualTo(new[] { "profile:view", "wishlist:edit" }));
	}

	[TestCase(EventWorkflowState.Draft)]
	[TestCase(EventWorkflowState.Preparing)]
	public void Create_PostDraftState_ShowsChatsAndGiftReady(string eventState)
	{
		var keyboard = EventParticipantKeyboard.Create(eventState);

		Assert.That(keyboard, Is.Not.Null);
		var callbackData = keyboard!.InlineKeyboard.SelectMany(row => row).Select(button => button.CallbackData).ToArray();
		Assert.That(callbackData, Is.EqualTo(new[] { "chat:recipient", "chat:santa", "gift:ready" }));
	}

	[Test]
	public void Create_UnsupportedState_ReturnsNoKeyboard()
	{
		Assert.That(EventParticipantKeyboard.Create(EventWorkflowState.Voting), Is.Null);
	}
}
