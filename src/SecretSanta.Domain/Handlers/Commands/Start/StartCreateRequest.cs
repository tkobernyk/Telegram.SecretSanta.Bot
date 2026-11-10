using MediatR;
using SecretSanta.Domain.Models;

namespace SecretSanta.Domain.Handlers.Commands.Start;

public class StartCreateRequest : IRequest<StartResponse>
{
	public string EventId { get; set; } = string.Empty;
	public string TelegramUserId { get; set; } = string.Empty;
	public string? DisplayName { get; set; }
	public TelegramMessage? Message { get; set; }
}
