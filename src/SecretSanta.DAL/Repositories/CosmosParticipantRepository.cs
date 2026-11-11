using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Abstractions.Repositories;
using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Repositories;

public class CosmosParticipantRepository : IParticipantRepository
{
	const string GetByTelegramUserIdQuery = "SELECT * FROM c WHERE c.eventId = @eventId AND c.TelegramUserId = @telegramUserId";
	const string GetAllByEventQuery = "SELECT * FROM c WHERE c.eventId = @eventId";

	readonly IDbExecutor<Participant> _executor;

	public CosmosParticipantRepository(IDbExecutor<Participant> executor)
	{
		_executor = executor;
	}

	public async Task<Participant?> GetByTelegramUserIdAsync(string eventId, string telegramUserId, CancellationToken cancellationToken = default)
	{
		var items = await _executor.QueryAsync(GetByTelegramUserIdQuery, new { eventId, telegramUserId }, cancellationToken);
		return items.FirstOrDefault();
	}

	public async Task CreateAsync(Participant participant, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(participant.EventId)) throw new ArgumentException("participant.EventId is required");
		await _executor.CreateItemAsync(participant, participant.EventId, cancellationToken);
	}

	public async Task UpdateAsync(Participant participant, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(participant.EventId)) throw new ArgumentException("participant.EventId is required");
		await _executor.UpsertItemAsync(participant, participant.EventId, cancellationToken);
	}

	public async Task<IEnumerable<Participant>> GetAllByEventAsync(string eventId, CancellationToken cancellationToken = default)
	{
		return await _executor.QueryAsync(GetAllByEventQuery, new { eventId }, cancellationToken);
	}
}






































































































































































































































































































