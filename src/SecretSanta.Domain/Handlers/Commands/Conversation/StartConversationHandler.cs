using MediatR;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.Domain.Enums;
using SecretSanta.DAL.Abstractions.Repositories;

namespace SecretSanta.Domain.Handlers.Commands.Conversation;

public class StartConversationHandler(IConversationOrchestrator _orchestrator, IEventRepository _eventRepository, IAssignmentRepository _assignmentRepository) : IRequestHandler<StartConversationRequest, CommandResponse>
{
	public async Task<CommandResponse> Handle(StartConversationRequest request, CancellationToken cancellationToken)
	{
		// Only allow after assignments are published
		var eventEntity = await _eventRepository.GetByEventIdAsync(request.EventId, cancellationToken);
		if (eventEntity == null)
		{
			return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Подію не знайдено." };
		}

		if (!Enum.TryParse<EventState>(eventEntity.State, true, out var state))
		{
			state = EventState.Registration;
		}

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
				var assignment = await _assignmentRepository.GetAssignmentForRecipientAsync(request.EventId, initiatorId, cancellationToken);
				santaId = assignment?.SantaTelegramUserId;
				if (santaId == null)
				{
					return new CommandResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Секретний Миколайко не знайдений для вас." };
				}
				recipientId = initiatorId;
				break;
			}
			case "Recipient":
			{
				// user wants to message their recipient; find who their recipient is
				var assignment = await _assignmentRepository.GetAssignmentForSantaAsync(request.EventId, initiatorId, cancellationToken);
				recipientId = assignment?.RecipientTelegramUserId;
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
		await _orchestrator.StartConversationAsync(request.EventId, santaId?.ToString() ?? string.Empty, recipientId?.ToString() ?? string.Empty, request.InitiatorTelegramUserId, cancellationToken);
		return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NoContent };
	}
}
