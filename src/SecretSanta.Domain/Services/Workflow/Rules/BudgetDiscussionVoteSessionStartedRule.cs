using SecretSanta.Domain.Enums;

namespace SecretSanta.Domain.Services.Workflow.Rules;

public sealed class BudgetDiscussionVoteSessionStartedRule : EventWorkflowRuleBase
{
	protected override string CurrentState => EventWorkflowState.BudgetDiscussion;
	protected override string Trigger => nameof(EventWorkflowTrigger.VoteSessionStarted);
	protected override string NextState => EventWorkflowState.Voting;
}
