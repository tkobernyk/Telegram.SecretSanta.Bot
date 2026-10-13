using MediatR;
using SecretSanta.Domain.Models;

namespace SecretSanta.Domain.Handlers.TelegramEndpoint;

public class TelegramEndpointRequest : IRequest<TelegramEndpointResponse>
{
	public string EventId { get; set; } = string.Empty;
	public TelegramMessage? Message { get; set; }
}
