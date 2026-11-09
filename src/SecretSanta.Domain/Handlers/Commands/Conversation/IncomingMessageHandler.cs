using MediatR;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Handlers.Common;

namespace SecretSanta.Domain.Handlers.Commands.Conversation;

public class IncomingMessageHandler(IConversationOrchestrator _orchestrator) : IRequestHandler<IncomingMessageRequest, CommandResponse>
{
	public async Task<CommandResponse> Handle(IncomingMessageRequest request, CancellationToken cancellationToken)
	{
		var sender = request.Message?.From?.Id.ToString() ?? string.Empty;
		var text = request.Message?.Text ?? string.Empty;

		var resp = await _orchestrator.HandleIncomingMessageAsync(request.EventId, sender, text, cancellationToken);
		if (resp != null)
		{
			return new CommandResponse { StatusCode = System.Net.HttpStatusCode.OK, ResponseMessage = resp };
		}
		return new CommandResponse { StatusCode = System.Net.HttpStatusCode.OK };
	}
}
