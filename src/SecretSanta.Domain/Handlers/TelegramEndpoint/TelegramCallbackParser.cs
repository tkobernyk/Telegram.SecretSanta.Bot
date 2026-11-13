namespace SecretSanta.Domain.Handlers.TelegramEndpoint;

internal static class TelegramCallbackParser
{
	public static TelegramCallbackParseResult Parse(string callbackData)
	{
		if (string.IsNullOrWhiteSpace(callbackData))
		{
			return new TelegramCallbackParseResult { CallbackType = TelegramCallbackType.Unknown };
		}

		if (callbackData.StartsWith("budget:", StringComparison.OrdinalIgnoreCase)
			&& long.TryParse(callbackData["budget:".Length..], out var budgetValue))
		{
			return new TelegramCallbackParseResult { CallbackType = TelegramCallbackType.BudgetVote, BudgetValue = budgetValue };
		}

		if (callbackData.StartsWith("calendar:", StringComparison.OrdinalIgnoreCase))
		{
			return new TelegramCallbackParseResult { CallbackType = TelegramCallbackType.Calendar };
		}

		return callbackData switch
		{
			"vote:start" => new TelegramCallbackParseResult { CallbackType = TelegramCallbackType.VoteStart },
			"gift:ready" => new TelegramCallbackParseResult { CallbackType = TelegramCallbackType.GiftReady },
			"gift:progress" => new TelegramCallbackParseResult { CallbackType = TelegramCallbackType.GiftProgress },
			"chat:recipient" => new TelegramCallbackParseResult { CallbackType = TelegramCallbackType.ChatRecipient },
			"chat:santa" => new TelegramCallbackParseResult { CallbackType = TelegramCallbackType.ChatSanta },
			"profile:view" => new TelegramCallbackParseResult { CallbackType = TelegramCallbackType.ProfileView },
			"wishlist:edit" => new TelegramCallbackParseResult { CallbackType = TelegramCallbackType.WishlistEdit },
			"conversation:end" => new TelegramCallbackParseResult { CallbackType = TelegramCallbackType.ConversationEnd },
			"start:create" => new TelegramCallbackParseResult { CallbackType = TelegramCallbackType.StartCreate },
			"start:join" => new TelegramCallbackParseResult { CallbackType = TelegramCallbackType.StartJoin },
			_ => new TelegramCallbackParseResult { CallbackType = TelegramCallbackType.Unknown }
		};
	}
}