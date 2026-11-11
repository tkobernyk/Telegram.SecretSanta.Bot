using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Enums;
using SecretSanta.Domain.Models.Workflow;

namespace SecretSanta.Domain.Abstractions;

public interface IEventStateMachine
{
	EventStateTransitionResult Transition(Event ev, EventWorkflowTrigger trigger, EventStateTransitionContext? context = null);
}
