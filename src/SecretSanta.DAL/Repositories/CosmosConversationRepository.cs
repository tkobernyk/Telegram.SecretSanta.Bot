using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;
using Microsoft.Azure.Cosmos;

namespace SecretSanta.DAL.Repositories;

public class CosmosConversationRepository : IConversationRepository
{
	readonly IExecutor _executor;

	public CosmosConversationRepository(IExecutor executor)
	{
		_executor = executor;
	}

	public async Task CreateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
	{
		conversation.CreatedAtUtc = DateTime.UtcNow;
		conversation.UpdatedAtUtc = DateTime.UtcNow;
		await _executor.CreateItemAsync(conversation, conversation.EventId, cancellationToken);
	}

	public async Task<Conversation?> GetByRecipientAsync(string eventId, string recipientTelegramUserId, CancellationToken cancellationToken = default)
	{
		var q = new QueryDefinition("SELECT * FROM c WHERE c.eventId = @eventId AND c.RecipientTelegramUserId = @recipient ORDER BY c.UpdatedAtUtc DESC")
			.WithParameter("@eventId", eventId)
			.WithParameter("@recipient", recipientTelegramUserId);

		var conv = await _executor.GetItemByIdAsync<Conversation>(q, eventId, cancellationToken);
		return conv;
	}

	public async Task AddMessageAsync(ConversationMessage message, CancellationToken cancellationToken = default)
	{
		message.CreatedAtUtc = DateTime.UtcNow;
		await _executor.CreateItemAsync(message, message.EventId, cancellationToken);
	}

	public async Task UpdateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
	{
		conversation.UpdatedAtUtc = DateTime.UtcNow;
		await _executor.UpsertItemAsync(conversation, conversation.EventId, cancellationToken);
	}
}
