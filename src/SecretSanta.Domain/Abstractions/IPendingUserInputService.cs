using SecretSanta.Domain.Models;

namespace SecretSanta.Domain.Abstractions;

public interface IPendingUserInputService
{
	void StartConversation(string telegramUserId, ConversationState state);
	bool TryGetConversation(string telegramUserId, out ConversationState? state);
	void UpdateConversation(string telegramUserId, ConversationState state);
	void EndConversation(string telegramUserId);
	void SetActiveEventId(string telegramUserId, string eventId);
	bool TryGetActiveEventId(string telegramUserId, out string eventId);
	void ClearActiveEventId(string telegramUserId);
	Task SetActiveEventIdAsync(string telegramUserId, string eventId, CancellationToken cancellationToken = default);
	Task<string?> GetActiveEventIdAsync(string telegramUserId, CancellationToken cancellationToken = default);
	Task ClearActiveEventIdAsync(string telegramUserId, CancellationToken cancellationToken = default);
}

