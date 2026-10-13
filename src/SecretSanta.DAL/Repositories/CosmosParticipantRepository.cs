using Microsoft.Azure.Cosmos;
using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Repositories;

public class CosmosParticipantRepository : IParticipantRepository
{
	#region
	const string GetByTelegramUserIdQuery = "SELECT * FROM c WHERE c.eventId = @eventId AND c.TelegramUserId = @telegramUserId";
	#endregion

	readonly IExecutor _executor;

	public CosmosParticipantRepository(IExecutor executor)
	{
		_executor = executor;
	}

	public async Task<Participant?> GetByTelegramUserIdAsync(string eventId, string telegramUserId, CancellationToken cancellationToken = default)
	{
		var query = new QueryDefinition(GetByTelegramUserIdQuery)
			.WithParameter("@eventId", eventId)
			.WithParameter("@telegramUserId", telegramUserId);

		var item = await _executor.GetItemByIdAsync<Participant>(query, eventId, cancellationToken);
		return item;
	}

	public async Task CreateAsync(Participant participant, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(participant.EventId)) throw new ArgumentException("participant.EventId is required");
		await _executor.CreateItemAsync(participant, participant.EventId, cancellationToken: cancellationToken);
	}
}
