using MediatR;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.Domain.Enums;

namespace SecretSanta.Domain.Handlers.Commands.Conversation;

public class StartConversationHandler(IConversationOrchestrator _orchestrator, IEventService _eventService, IAssignmentService _assignmentService) : IRequestHandler<StartConversationRequest, CommandResponse>
{
	public async Task<CommandResponse> Handle(StartConversationRequest request, CancellationToken cancellationToken)
	{
		// Only allow after assignments are published
		var state = await _eventService.GetEventStateAsync(request.EventId, cancellationToken);
		if (state != EventState.Draft && state != EventState.Preparing && state != EventState.AwaitingDelivery && state != EventState.Done && state != EventState.Completed)
		{
			return new CommandResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Команди доступні лише після завершення жеребкування." };
		}

		// Determine actual santa and recipient based on StartTarget and initiator
		long initiatorId = long.TryParse(request.InitiatorTelegramUserId, out var id) ? id : 0;
		long? santaId = null;
		long? recipientId = null;

		switch (request.StartTarget)
		{
			case "Santa":
			{
				// user wants to message their Santa; find who the Santa is for this initiator
				santaId = await _assignmentService.GetSantaForRecipientAsync(request.EventId, initiatorId, cancellationToken);
				if (santaId == null)
				{
					return new CommandResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Секретний Санта не знайдений для вас." };
				}
				recipientId = initiatorId;
				break;
			}
			case "Recipient":
			{
				// user wants to message their recipient; find who their recipient is
				recipientId = await _assignmentService.GetRecipientForSantaAsync(request.EventId, initiatorId, cancellationToken);
				if (recipientId == null)
				{
					return new CommandResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Отримувач не знайдений для вас." };
				}
				santaId = initiatorId;
				break;
			}
			default:
				return new CommandResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Неправильний параметр запуску діалогу." };
		}

		// Do not reveal santa identity in any response. Start orchestration which will route messages anonymously via orchestrator.
		await _orchestrator.StartConversationAsync(request.EventId, santaId?.ToString() ?? string.Empty, recipientId?.ToString() ?? string.Empty, cancellationToken);
		return new CommandResponse { StatusCode = System.Net.HttpStatusCode.OK, ResponseMessage = "Анонімний діалог розпочато. Ви можете писати повідомлення, які буде переслано анонімно." };
	}
}
