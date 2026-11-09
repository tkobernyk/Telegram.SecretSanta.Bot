using MediatR;

namespace SecretSanta.Domain.Handlers.Commands.Participant;

public class MarkReadyRequest : IRequest<SecretSanta.Domain.Handlers.Common.CommandResponse>
{
	public string EventId { get; set; } = string.Empty;
	public long TelegramUserId { get; set; }
	public string GiftStatus { get; set; } = "Ready";
}
