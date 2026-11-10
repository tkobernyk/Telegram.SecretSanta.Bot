using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Abstractions;

public interface IConversationRepository
{
	Task CreateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default);
	Task<Conversation?> GetByRecipientAsync(string eventId, string recipientTelegramUserId, CancellationToken cancellationToken = default);
	Task<Conversation?> GetActiveForParticipantAsync(string eventId, string telegramUserId, CancellationToken cancellationToken = default);
	Task AddMessageAsync(ConversationMessage message, CancellationToken cancellationToken = default);
	Task UpdateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default);
}
