namespace SecretSanta.Domain.Models.Workflow;

public sealed class EventStateTransitionContext
{
	public long? ChangedByTelegramId { get; init; }
	public string? Note { get; init; }
	public DateTime ChangedAtUtc { get; init; } = DateTime.UtcNow;
}
