using SecretSanta.Domain.Enums;

namespace SecretSanta.Domain.Services.Workflow.Rules;

public sealed class VotingBudgetConsensusNotReachedRule : EventWorkflowRuleBase
{
	protected override string CurrentState => EventWorkflowState.Voting;
	protected override string Trigger => nameof(EventWorkflowTrigger.BudgetConsensusNotReached);
	protected override string NextState => EventWorkflowState.BudgetDiscussion;
}
