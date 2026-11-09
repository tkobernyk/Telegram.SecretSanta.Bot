using MediatR;

namespace SecretSanta.Domain.Handlers.Commands.Conversation;

public class CancelConversationRequest : IRequest<Unit>
{
	public string EventId { get; set; } = string.Empty;
	public string InitiatorTelegramUserId { get; set; } = string.Empty;
}
