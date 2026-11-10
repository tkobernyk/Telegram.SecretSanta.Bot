using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Repositories;

public class CosmosEventRepository : IEventRepository
{
	private readonly IExecutor _executor;
	private readonly string _container = "Events";

	public CosmosEventRepository(IExecutor executor)
	{
		_executor = executor;
	}

	public Task CreateAsync(Event ev, CancellationToken cancellationToken = default)
	{
		return _executor.CreateItemAsync(_container, ev.EventId, ev, cancellationToken);
	}

	public async Task<Event?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken = default)
	{
		var q = $"SELECT * FROM c WHERE c.eventId = @eventId";
		var res = await _executor.QueryAsync<Event>(_container, q, new { eventId }, cancellationToken);
		return res.FirstOrDefault();
	}

	public async Task<Event?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
	{
		var q = "SELECT * FROM c WHERE c.joinCode = @joinCode";
		var res = await _executor.QueryAsync<Event>(_container, q, new { joinCode }, cancellationToken);
		return res.FirstOrDefault();
	}

	public Task UpdateAsync(Event ev, CancellationToken cancellationToken = default)
	{
		return _executor.UpsertItemAsync(_container, ev.EventId, ev, cancellationToken);
	}

	public async Task<IEnumerable<Event>> QueryByStateAsync(string state, CancellationToken cancellationToken = default)
	{
		var q = $"SELECT * FROM c WHERE c.state = @state";
		return await _executor.QueryAsync<Event>(_container, q, new { state }, cancellationToken);
	}
}
