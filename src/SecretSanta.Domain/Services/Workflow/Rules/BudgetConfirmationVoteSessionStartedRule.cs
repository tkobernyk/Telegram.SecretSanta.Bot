using SecretSanta.Domain.Enums;

namespace SecretSanta.Domain.Services.Workflow.Rules;

public sealed class BudgetConfirmationVoteSessionStartedRule : EventWorkflowRuleBase
{
	protected override string CurrentState => EventWorkflowState.BudgetConfirmation;
	protected override string Trigger => nameof(EventWorkflowTrigger.VoteSessionStarted);
	protected override string NextState => EventWorkflowState.Voting;
}