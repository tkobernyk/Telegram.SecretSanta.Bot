using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Abstractions.Repositories;
using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Repositories;

public class CosmosConversationRepository : IConversationRepository
{
	readonly IDbExecutor<Conversation> _conversationExecutor;
	readonly IDbExecutor<ConversationMessage> _messageExecutor;

	public CosmosConversationRepository(IDbExecutor<Conversation> conversationExecutor, IDbExecutor<ConversationMessage> messageExecutor)
	{
		_conversationExecutor = conversationExecutor;
		_messageExecutor = messageExecutor;
	}

	public async Task CreateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
	{
		conversation.CreatedAtUtc = DateTime.UtcNow;
		conversation.UpdatedAtUtc = DateTime.UtcNow;
		await _conversationExecutor.CreateItemAsync(conversation, conversation.EventId, cancellationToken);
	}

	public async Task<Conversation?> GetByRecipientAsync(string eventId, string recipientTelegramUserId, CancellationToken cancellationToken = default)
	{
		var results = await _conversationExecutor.QueryAsync(
			"SELECT * FROM c WHERE c.eventId = @eventId AND c.recipientTelegramUserId = @recipient ORDER BY c.updatedAtUtc DESC",
			new { eventId, recipient = recipientTelegramUserId },
			cancellationToken);

		return results.OrderByDescending(x => x.UpdatedAtUtc).FirstOrDefault();
	}

	public async Task<Conversation?> GetActiveForParticipantAsync(string eventId, string telegramUserId, CancellationToken cancellationToken = default)
	{
		var results = await _conversationExecutor.QueryAsync(
			"SELECT * FROM c WHERE c.eventId = @eventId AND c.status = @status AND (c.santaTelegramUserId = @telegramUserId OR c.recipientTelegramUserId = @telegramUserId)",
			new { eventId, status = "Active", telegramUserId },
			cancellationToken);
		return results.OrderByDescending(x => x.UpdatedAtUtc).FirstOrDefault();
	}

	public async Task AddMessageAsync(ConversationMessage message, CancellationToken cancellationToken = default)
	{
		message.CreatedAtUtc = DateTime.UtcNow;
		await _messageExecutor.CreateItemAsync(message, message.EventId, cancellationToken);
	}

	public async Task UpdateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
	{
		conversation.UpdatedAtUtc = DateTime.UtcNow;
		await _conversationExecutor.UpsertItemAsync(conversation, conversation.EventId, cancellationToken);
	}
}
