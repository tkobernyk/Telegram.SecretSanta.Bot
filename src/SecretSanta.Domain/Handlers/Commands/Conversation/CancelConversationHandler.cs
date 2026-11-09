using MediatR;
using SecretSanta.Domain.Abstractions;

namespace SecretSanta.Domain.Handlers.Commands.Conversation;

public class CancelConversationHandler(IConversationOrchestrator _orchestrator) : IRequestHandler<CancelConversationRequest, Unit>
{
	public async Task<Unit> Handle(CancelConversationRequest request, CancellationToken cancellationToken)
	{
		await _orchestrator.EndConversationAsync(request.EventId, request.InitiatorTelegramUserId, cancellationToken);
		return Unit.Value;
	}
}
