using MediatR;

namespace SecretSanta.Domain.Handlers.Commands.Conversation;

public class StartConversationRequest : IRequest<SecretSanta.Domain.Handlers.Common.CommandResponse>
{
	public string EventId { get; set; } = string.Empty;
	public string SantaTelegramUserId { get; set; } = string.Empty;
	public string RecipientTelegramUserId { get; set; } = string.Empty;
	// The Telegram user id string of the user who initiated the command.
	public string InitiatorTelegramUserId { get; set; } = string.Empty;
	// StartTarget indicates whether the initiator requested the Santa or the Recipient. Use "Santa" or "Recipient".
	public string StartTarget { get; set; } = string.Empty;
}
