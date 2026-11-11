using SecretSanta.Domain.Enums;

namespace SecretSanta.Domain.Services.Workflow.Rules;

public sealed class DraftGiftProgressUpdatedRule : EventWorkflowRuleBase
{
	protected override string CurrentState => EventWorkflowState.Draft;
	protected override string Trigger => nameof(EventWorkflowTrigger.GiftProgressUpdated);
	protected override string NextState => EventWorkflowState.Preparing;
}
