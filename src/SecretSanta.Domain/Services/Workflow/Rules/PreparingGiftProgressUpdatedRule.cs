using SecretSanta.Domain.Enums;

namespace SecretSanta.Domain.Services.Workflow.Rules;

public sealed class PreparingGiftProgressUpdatedRule : EventWorkflowRuleBase
{
	protected override string CurrentState => EventWorkflowState.Preparing;
	protected override string Trigger => nameof(EventWorkflowTrigger.GiftProgressUpdated);
	protected override string NextState => EventWorkflowState.Preparing;
}
