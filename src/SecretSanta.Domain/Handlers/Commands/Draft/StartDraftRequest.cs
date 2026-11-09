using MediatR;

namespace SecretSanta.Domain.Handlers.Commands.Draft;

public class StartDraftRequest : IRequest<SecretSanta.Domain.Handlers.Common.CommandResponse>
{
	public string EventId { get; set; } = string.Empty;
	public long InitiatorTelegramId { get; set; }
}
