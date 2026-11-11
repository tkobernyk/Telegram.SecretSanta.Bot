using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Abstractions.Repositories;
using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Repositories;

public class CosmosEventRepository : IEventRepository
{
	readonly IDbExecutor<Event> _executor;

	public CosmosEventRepository(IDbExecutor<Event> executor)
	{
		_executor = executor;
	}

	public Task CreateAsync(Event ev, CancellationToken cancellationToken = default)
	{
		return _executor.CreateItemAsync(ev, ev.EventId, cancellationToken);
	}

	public async Task<Event?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken = default)
	{
		var q = $"SELECT * FROM c WHERE c.eventId = @eventId";
		var res = await _executor.QueryAsync(q, new { eventId }, cancellationToken);
		return res.FirstOrDefault();
	}

	public async Task<Event?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
	{
		var q = "SELECT * FROM c WHERE c.joinCode = @joinCode";
		var res = await _executor.QueryAsync(q, new { joinCode }, cancellationToken);
		return res.FirstOrDefault();
	}

	public Task UpdateAsync(Event ev, CancellationToken cancellationToken = default)
	{
		return _executor.UpsertItemAsync(ev, ev.EventId, cancellationToken);
	}

	public async Task<IEnumerable<Event>> QueryByStateAsync(string state, CancellationToken cancellationToken = default)
	{
		var q = $"SELECT * FROM c WHERE c.state = @state";
		return await _executor.QueryAsync(q, new { state }, cancellationToken);
	}
}
