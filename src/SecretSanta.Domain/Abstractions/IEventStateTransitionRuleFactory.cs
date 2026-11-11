namespace SecretSanta.Domain.Abstractions;

public interface IEventStateTransitionRuleFactory
{
	IEventStateTransitionRule? Resolve(string currentState, string trigger);
}
