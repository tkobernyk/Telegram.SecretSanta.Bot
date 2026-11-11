using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Enums;
using SecretSanta.Domain.Models.Workflow;

namespace SecretSanta.Domain.Services.Workflow;

public sealed class EventStateMachine : IEventStateMachine
{
	readonly IEventStateTransitionRuleFactory _ruleFactory;

	public EventStateMachine(IEventStateTransitionRuleFactory ruleFactory)
	{
		_ruleFactory = ruleFactory;
	}

	public EventStateTransitionResult Transition(Event ev, EventWorkflowTrigger trigger, EventStateTransitionContext? context = null)
	{
		var currentState = string.IsNullOrWhiteSpace(ev.State) ? EventWorkflowState.Registration : ev.State;
		var triggerKey = trigger.ToString();
		var rule = _ruleFactory.Resolve(currentState, triggerKey);
		if (rule == null)
		{
			return EventStateTransitionResult.Failure(currentState, $"Transition is not allowed: {currentState} --({triggerKey})-> ?");
		}

		var transitionContext = context ?? new EventStateTransitionContext();
		var result = rule.Apply(ev, transitionContext);
		if (!result.Changed)
		{
			return result;
		}

		ev.State = result.NewState;
		ev.UpdatedAtUtc = transitionContext.ChangedAtUtc;
		ev.History.Add(new EventStateChange
		{
			State = result.NewState,
			ChangedBy = transitionContext.ChangedByTelegramId,
			ChangedAtUtc = transitionContext.ChangedAtUtc,
			Note = transitionContext.Note ?? triggerKey
		});

		return result;
	}
}
