namespace SecretSanta.Domain.Models.Workflow;

public sealed class EventStateTransitionResult
{
	public required string NewState { get; init; }
	public bool Changed { get; init; }
	public string? Error { get; init; }

	public static EventStateTransitionResult Success(string newState, bool changed) => new() { NewState = newState, Changed = changed };

	public static EventStateTransitionResult Failure(string currentState, string error) => new() { NewState = currentState, Changed = false, Error = error };
}
