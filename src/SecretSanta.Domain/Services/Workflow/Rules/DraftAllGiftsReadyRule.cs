using SecretSanta.Domain.Enums;

namespace SecretSanta.Domain.Services.Workflow.Rules;

public sealed class DraftAllGiftsReadyRule : EventWorkflowRuleBase
{
	protected override string CurrentState => EventWorkflowState.Draft;
	protected override string Trigger => nameof(EventWorkflowTrigger.AllGiftsReady);
	protected override string NextState => EventWorkflowState.AwaitingDelivery;
}
