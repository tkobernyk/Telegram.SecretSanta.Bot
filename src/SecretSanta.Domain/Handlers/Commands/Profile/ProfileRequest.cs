using MediatR;
using SecretSanta.Domain.Models;

namespace SecretSanta.Domain.Handlers.Commands.Profile;

public class ProfileRequest : IRequest<ProfileResponse>
{
	public string EventId { get; set; } = string.Empty;
	public TelegramMessage? Message { get; set; }
}
