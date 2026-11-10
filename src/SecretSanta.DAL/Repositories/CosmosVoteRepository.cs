using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Repositories;

public class CosmosVoteRepository : IVoteRepository
{
	readonly IDbExecutor<VoteSession> _sessionExecutor;
	readonly IDbExecutor<Vote> _voteExecutor;

	public CosmosVoteRepository(IDbExecutor<VoteSession> sessionExecutor, IDbExecutor<Vote> voteExecutor)
	{
		_sessionExecutor = sessionExecutor;
		_voteExecutor = voteExecutor;
	}

	public Task CreateVoteSessionAsync(VoteSession session, CancellationToken cancellationToken = default)
	{
		return _sessionExecutor.CreateItemAsync(session, session.EventId, cancellationToken);
	}

	public async Task<VoteSession?> GetVoteSessionAsync(string eventId, string sessionId, CancellationToken cancellationToken = default)
	{
		var q = "SELECT * FROM c WHERE c.eventId = @eventId AND c.id = @id";
		var res = await _sessionExecutor.QueryAsync(q, new { eventId, id = sessionId }, cancellationToken);
		return res.FirstOrDefault();
	}

	public Task CreateVoteAsync(Vote vote, CancellationToken cancellationToken = default)
	{
		return _voteExecutor.UpsertItemAsync(vote, vote.EventId, cancellationToken);
	}

	public async Task<IEnumerable<Vote>> GetVotesForSessionAsync(string eventId, string sessionId, CancellationToken cancellationToken = default)
	{
		var q = "SELECT * FROM c WHERE c.eventId = @eventId AND c.voteSessionId = @sessionId";
		return await _voteExecutor.QueryAsync(q, new { eventId, sessionId }, cancellationToken);
	}

	public Task CloseVoteSessionAsync(VoteSession session, CancellationToken cancellationToken = default)
	{
		session.Status = "Closed";
		session.ClosedAtUtc = System.DateTime.UtcNow;
		return _sessionExecutor.UpsertItemAsync(session, session.EventId, cancellationToken);
	}
}
