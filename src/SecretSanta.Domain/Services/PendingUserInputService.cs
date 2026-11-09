using System.Collections.Concurrent;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Models;

namespace SecretSanta.Domain.Abstractions;

public class PendingUserInputService : IPendingUserInputService
{
	readonly ConcurrentDictionary<string, ConversationState> _dict = new();

	public void StartConversation(string telegramUserId, ConversationState state)
	{
		_dict[telegramUserId] = state;
	}

	public bool TryGetConversation(string telegramUserId, out ConversationState? state)
	{
		if (_dict.TryGetValue(telegramUserId, out var s))
		{
			state = s;
			return true;
		}
		state = null;
		return false;
	}

	public void UpdateConversation(string telegramUserId, ConversationState state)
	{
		_dict[telegramUserId] = state;
	}

	public void EndConversation(string telegramUserId)
	{
		_dict.TryRemove(telegramUserId, out _);
	}
}
