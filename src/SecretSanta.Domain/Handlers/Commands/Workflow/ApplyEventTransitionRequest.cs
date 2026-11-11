using MediatR;
using SecretSanta.Domain.Enums;
using SecretSanta.Domain.Handlers.Common;

namespace SecretSanta.Domain.Handlers.Commands.Workflow;

public sealed class ApplyEventTransitionRequest : IRequest<CommandResponse>
{
	public string EventId { get; set; } = string.Empty;
	public EventWorkflowTrigger Trigger { get; set; }
	public long? ChangedByTelegramId { get; set; }
	public string? Note { get; set; }
}