using SecretSanta.Domain.Enums;

namespace SecretSanta.Domain.Services.Workflow.Rules;

public sealed class AwaitingDeliveryDeliveryInfoProvidedRule : EventWorkflowRuleBase
{
	protected override string CurrentState => EventWorkflowState.AwaitingDelivery;
	protected override string Trigger => nameof(EventWorkflowTrigger.DeliveryInfoProvided);
	protected override string NextState => EventWorkflowState.Done;
}
