using SecretSanta.Domain.Models;

namespace SecretSanta.Domain.Abstractions;

public interface IPendingUserInputService
{
	void StartConversation(string telegramUserId, ConversationState state);
	bool TryGetConversation(string telegramUserId, out ConversationState? state);
	void UpdateConversation(string telegramUserId, ConversationState state);
	void EndConversation(string telegramUserId);
}
