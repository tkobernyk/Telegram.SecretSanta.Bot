using MediatR;

namespace SecretSanta.Domain.Handlers.Commands.Start;

public class StartJoinRequest : IRequest<StartResponse>
{
	public string EventId { get; set; } = string.Empty;
	public string TelegramUserId { get; set; } = string.Empty;
	public string? JoinCode { get; set; }
	public string? DisplayName { get; set; }
}
