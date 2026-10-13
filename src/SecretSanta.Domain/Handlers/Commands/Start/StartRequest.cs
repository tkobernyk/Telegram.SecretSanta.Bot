using MediatR;
using SecretSanta.Domain.Models;

namespace SecretSanta.Domain.Handlers.Commands.Start;

public class StartRequest : IRequest<StartResponse>
{
	public string EventId { get; set; } = string.Empty;
	public TelegramMessage? Message { get; set; }
}
