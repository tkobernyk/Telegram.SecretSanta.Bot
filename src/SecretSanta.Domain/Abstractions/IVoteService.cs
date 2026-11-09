using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Services;

namespace SecretSanta.Domain.Abstractions;

public interface IVoteService
{
	Task<VoteSession> CreateVoteSessionAsync(string eventId, VoteSession session, CancellationToken cancellationToken = default);
	Task SubmitVoteAsync(string eventId, string sessionId, Vote vote, CancellationToken cancellationToken = default);
	Task<string?> AggregateResultAsync(string eventId, string sessionId, CancellationToken cancellationToken = default);
	Task<BudgetVoteOutcome> EvaluateBudgetVotesAsync(string eventId, string sessionId, CancellationToken cancellationToken = default);
}
