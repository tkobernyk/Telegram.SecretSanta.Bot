using MediatR;
using SecretSanta.Domain.Models;

namespace SecretSanta.Domain.Handlers.Commands.Wishlist;

public class WishlistRequest : IRequest<WishlistResponse>
{
	public string EventId { get; set; } = string.Empty;
	public TelegramMessage? Message { get; set; }
}
