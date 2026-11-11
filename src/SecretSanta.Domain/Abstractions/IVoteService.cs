using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Services;

namespace SecretSanta.Domain.Abstractions;

public interface IVoteService
{
	VoteSession PrepareVoteSession(VoteSession session);
	BudgetVoteOutcome EvaluateBudgetVotes(IEnumerable<Vote> votes, int expectedParticipantsCount);
}
