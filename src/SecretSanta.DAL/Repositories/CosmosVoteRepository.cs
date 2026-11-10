using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Repositories;

public class CosmosVoteRepository : IVoteRepository
{
	private readonly IExecutor _executor;
	private readonly string _sessionContainer = "VoteSessions";
	private readonly string _voteContainer = "Votes";

	public CosmosVoteRepository(IExecutor executor)
	{
		_executor = executor;
	}

	public Task CreateVoteSessionAsync(VoteSession session, CancellationToken cancellationToken = default)
	{
		return _executor.CreateItemAsync(_sessionContainer, session.EventId, session, cancellationToken);
	}

	public async Task<VoteSession?> GetVoteSessionAsync(string eventId, string sessionId, CancellationToken cancellationToken = default)
	{
		var q = "SELECT * FROM c WHERE c.eventId = @eventId AND c.id = @id";
		var res = await _executor.QueryAsync<VoteSession>(_sessionContainer, q, new { eventId, id = sessionId }, cancellationToken);
		return res.FirstOrDefault();
	}

	public Task CreateVoteAsync(Vote vote, CancellationToken cancellationToken = default)
	{
		return _executor.UpsertItemAsync(_voteContainer, vote.EventId, vote, cancellationToken);
	}

	public async Task<IEnumerable<Vote>> GetVotesForSessionAsync(string eventId, string sessionId, CancellationToken cancellationToken = default)
	{
		var q = "SELECT * FROM c WHERE c.eventId = @eventId AND c.voteSessionId = @sessionId";
		return await _executor.QueryAsync<Vote>(_voteContainer, q, new { eventId, sessionId }, cancellationToken);
	}

	public Task CloseVoteSessionAsync(VoteSession session, CancellationToken cancellationToken = default)
	{
		session.Status = "Closed";
		session.ClosedAtUtc = System.DateTime.UtcNow;
		return _executor.UpsertItemAsync(_sessionContainer, session.EventId, session, cancellationToken);
	}
}
