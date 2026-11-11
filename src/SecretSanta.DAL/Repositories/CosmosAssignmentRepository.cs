using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Abstractions.Repositories;
using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Repositories;

public class CosmosAssignmentRepository : IAssignmentRepository
{
	readonly IDbExecutor<Assignment> _executor;

	public CosmosAssignmentRepository(IDbExecutor<Assignment> executor)
	{
		_executor = executor;
	}

	public Task CreateAssignmentsAsync(IEnumerable<Assignment> assignments, CancellationToken cancellationToken = default)
	{
		var tasks = new List<Task>();
		foreach (var a in assignments)
		{
			tasks.Add(_executor.CreateItemAsync(a, a.EventId, cancellationToken));
		}
		return Task.WhenAll(tasks);
	}

	public async Task<IEnumerable<Assignment>> GetAssignmentsByEventAsync(string eventId, CancellationToken cancellationToken = default)
	{
		var q = "SELECT * FROM c WHERE c.eventId = @eventId";
		return await _executor.QueryAsync(q, new { eventId }, cancellationToken);
	}

	public async Task<Assignment?> GetAssignmentForRecipientAsync(string eventId, long recipientTelegramId, CancellationToken cancellationToken = default)
	{
		var q = "SELECT * FROM c WHERE c.eventId = @eventId AND c.recipientTelegramUserId = @recipient";
		var res = await _executor.QueryAsync(q, new { eventId, recipient = recipientTelegramId }, cancellationToken);
		return res.FirstOrDefault();
	}

	public async Task<Assignment?> GetAssignmentForSantaAsync(string eventId, long santaTelegramId, CancellationToken cancellationToken = default)
	{
		var q = "SELECT * FROM c WHERE c.eventId = @eventId AND c.santaTelegramUserId = @santa";
		var res = await _executor.QueryAsync(q, new { eventId, santa = santaTelegramId }, cancellationToken);
		return res.FirstOrDefault();
	}
}
