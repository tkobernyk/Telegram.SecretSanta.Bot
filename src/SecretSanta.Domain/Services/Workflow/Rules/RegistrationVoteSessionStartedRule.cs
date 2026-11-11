using SecretSanta.Domain.Enums;

namespace SecretSanta.Domain.Services.Workflow.Rules;

public sealed class RegistrationVoteSessionStartedRule : EventWorkflowRuleBase
{
	protected override string CurrentState => EventWorkflowState.Registration;
	protected override string Trigger => nameof(EventWorkflowTrigger.VoteSessionStarted);
	protected override string NextState => EventWorkflowState.Voting;
}
