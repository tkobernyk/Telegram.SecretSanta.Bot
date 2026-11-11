using SecretSanta.Domain.Enums;

namespace SecretSanta.Domain.Services.Workflow.Rules;

public sealed class VotingDraftStartedRule : EventWorkflowRuleBase
{
	protected override string CurrentState => EventWorkflowState.Voting;
	protected override string Trigger => nameof(EventWorkflowTrigger.DraftStarted);
	protected override string NextState => EventWorkflowState.Draft;
}
