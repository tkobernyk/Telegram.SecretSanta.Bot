using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;
using Microsoft.Azure.Cosmos;

namespace SecretSanta.DAL.Repositories;

public class CosmosConversationRepository : IConversationRepository
{
	readonly IExecutor _executor;
	readonly string _container = "Conversations";

	public CosmosConversationRepository(IExecutor executor)
	{
		_executor = executor;
	}

	public async Task CreateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
	{
		conversation.CreatedAtUtc = DateTime.UtcNow;
		conversation.UpdatedAtUtc = DateTime.UtcNow;
		await _executor.CreateItemAsync(_container, conversation.EventId, conversation, cancellationToken);
	}

	public async Task<Conversation?> GetByRecipientAsync(string eventId, string recipientTelegramUserId, CancellationToken cancellationToken = default)
	{
		var results = await _executor.QueryAsync<Conversation>(_container,
			"SELECT * FROM c WHERE c.eventId = @eventId AND c.recipientTelegramUserId = @recipient ORDER BY c.updatedAtUtc DESC",
			new { eventId, recipient = recipientTelegramUserId },
			cancellationToken);

		return results.OrderByDescending(x => x.UpdatedAtUtc).FirstOrDefault();
	}

	public async Task<Conversation?> GetActiveForParticipantAsync(string eventId, string telegramUserId, CancellationToken cancellationToken = default)
	{
		var results = await _executor.QueryAsync<Conversation>(_container,
			"SELECT * FROM c WHERE c.eventId = @eventId AND c.status = @status AND (c.santaTelegramUserId = @telegramUserId OR c.recipientTelegramUserId = @telegramUserId)",
			new { eventId, status = "Active", telegramUserId },
			cancellationToken);
		return results.OrderByDescending(x => x.UpdatedAtUtc).FirstOrDefault();
	}

	public async Task AddMessageAsync(ConversationMessage message, CancellationToken cancellationToken = default)
	{
		message.CreatedAtUtc = DateTime.UtcNow;
		await _executor.CreateItemAsync(_container, message.EventId, message, cancellationToken);
	}

	public async Task UpdateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
	{
		conversation.UpdatedAtUtc = DateTime.UtcNow;
		await _executor.UpsertItemAsync(_container, conversation.EventId, conversation, cancellationToken);
	}
}
