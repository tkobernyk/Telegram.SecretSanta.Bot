using SecretSanta.Domain.Enums;

namespace SecretSanta.Domain.Services.Workflow.Rules;

public sealed class DraftDraftStartedRule : EventWorkflowRuleBase
{
	protected override string CurrentState => EventWorkflowState.Draft;
	protected override string Trigger => nameof(EventWorkflowTrigger.DraftStarted);
	protected override string NextState => EventWorkflowState.Draft;
}
