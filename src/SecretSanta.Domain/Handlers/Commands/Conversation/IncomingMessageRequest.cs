using MediatR;
using SecretSanta.Domain.Models;

namespace SecretSanta.Domain.Handlers.Commands.Conversation;

public class IncomingMessageRequest : IRequest<SecretSanta.Domain.Handlers.Common.CommandResponse>
{
	public string EventId { get; set; } = string.Empty;
	public TelegramMessage? Message { get; set; }
}
