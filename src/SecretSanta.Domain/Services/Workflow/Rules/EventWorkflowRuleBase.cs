using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Models.Workflow;

namespace SecretSanta.Domain.Services.Workflow.Rules;

public abstract class EventWorkflowRuleBase : IEventStateTransitionRule
{
	public bool CanHandle(string currentState, string trigger)
	{
		return string.Equals(currentState, CurrentState, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(trigger, Trigger, StringComparison.Ordinal);
	}

	protected abstract string CurrentState { get; }
	protected abstract string Trigger { get; }
	protected abstract string NextState { get; }

	public virtual EventStateTransitionResult Apply(Event ev, EventStateTransitionContext context)
	{
		var changed = !string.Equals(ev.State, NextState, StringComparison.OrdinalIgnoreCase);
		return EventStateTransitionResult.Success(NextState, changed);
	}
}
