using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Abstractions.Repositories;

public interface IVoteRepository
{
	Task CreateVoteSessionAsync(VoteSession session, CancellationToken cancellationToken = default);
	Task<VoteSession?> GetVoteSessionAsync(string eventId, string sessionId, CancellationToken cancellationToken = default);
	Task CreateVoteAsync(Vote vote, CancellationToken cancellationToken = default);
	Task<IEnumerable<Vote>> GetVotesForSessionAsync(string eventId, string sessionId, CancellationToken cancellationToken = default);
	Task CloseVoteSessionAsync(VoteSession session, CancellationToken cancellationToken = default);
}
