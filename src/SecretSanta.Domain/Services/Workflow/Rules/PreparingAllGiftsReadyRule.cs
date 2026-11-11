using SecretSanta.Domain.Enums;

namespace SecretSanta.Domain.Services.Workflow.Rules;

public sealed class PreparingAllGiftsReadyRule : EventWorkflowRuleBase
{
	protected override string CurrentState => EventWorkflowState.Preparing;
	protected override string Trigger => nameof(EventWorkflowTrigger.AllGiftsReady);
	protected override string NextState => EventWorkflowState.AwaitingDelivery;
}
