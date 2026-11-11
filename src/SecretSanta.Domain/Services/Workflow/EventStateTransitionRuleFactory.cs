using SecretSanta.Domain.Abstractions;

namespace SecretSanta.Domain.Services.Workflow;

public sealed class EventStateTransitionRuleFactory : IEventStateTransitionRuleFactory
{
	readonly IReadOnlyList<IEventStateTransitionRule> _rules;

	public EventStateTransitionRuleFactory(IEnumerable<IEventStateTransitionRule> rules)
	{
		_rules = rules.ToList();
	}

	public IEventStateTransitionRule? Resolve(string currentState, string trigger)
	{
		return _rules.FirstOrDefault(rule => rule.CanHandle(currentState, trigger));
	}
}
