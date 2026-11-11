using System.Collections.Concurrent;
using SecretSanta.Domain.Models;

namespace SecretSanta.Domain.Handlers.Common;

public static class InteractionStateStore
{
	private static readonly ConcurrentDictionary<string, ConversationState> Conversations = new();
	private static readonly ConcurrentDictionary<string, string> ActiveEventIds = new();

	public static void StartConversation(string telegramUserId, ConversationState state)
	{
		Conversations[telegramUserId] = state;
	}

	public static bool TryGetConversation(string telegramUserId, out ConversationState? state)
	{
		if (Conversations.TryGetValue(telegramUserId, out var currentState))
		{
			state = currentState;
			return true;
		}

		state = null;
		return false;
	}

	public static void UpdateConversation(string telegramUserId, ConversationState state)
	{
		Conversations[telegramUserId] = state;
	}

	public static void EndConversation(string telegramUserId)
	{
		Conversations.TryRemove(telegramUserId, out _);
	}

	public static void SetActiveEventId(string telegramUserId, string eventId)
	{
		if (!string.IsNullOrWhiteSpace(telegramUserId) && !string.IsNullOrWhiteSpace(eventId))
		{
			ActiveEventIds[telegramUserId] = eventId;
		}
	}

	public static bool TryGetActiveEventId(string telegramUserId, out string eventId)
	{
		return ActiveEventIds.TryGetValue(telegramUserId, out eventId!);
	}

	public static void ClearActiveEventId(string telegramUserId)
	{
		ActiveEventIds.TryRemove(telegramUserId, out _);
	}
}
