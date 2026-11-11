using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Models.Workflow;

namespace SecretSanta.Domain.Abstractions;

public interface IEventStateTransitionRule
{
	bool CanHandle(string currentState, string trigger);
	EventStateTransitionResult Apply(Event ev, EventStateTransitionContext context);
}
