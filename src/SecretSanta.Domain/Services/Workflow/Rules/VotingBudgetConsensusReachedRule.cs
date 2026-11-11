using SecretSanta.Domain.Enums;

namespace SecretSanta.Domain.Services.Workflow.Rules;

public sealed class VotingBudgetConsensusReachedRule : EventWorkflowRuleBase
{
	protected override string CurrentState => EventWorkflowState.Voting;
	protected override string Trigger => nameof(EventWorkflowTrigger.BudgetConsensusReached);
	protected override string NextState => EventWorkflowState.Draft;
}
