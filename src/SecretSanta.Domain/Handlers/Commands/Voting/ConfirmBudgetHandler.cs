using MediatR;
using SecretSanta.Domain.Enums;
using SecretSanta.Domain.Handlers.Commands.Workflow;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.Domain.Abstractions;
using SecretSanta.DAL.Abstractions.Repositories;

namespace SecretSanta.Domain.Handlers.Commands.Voting;

public class ConfirmBudgetHandler : IRequestHandler<ConfirmBudgetRequest, CommandResponse>
{
	private readonly IParticipantRepository _participantRepo;
	private readonly IEventRepository _eventRepo;
	private readonly IMediator _mediator;

	public ConfirmBudgetHandler(IParticipantRepository participantRepo, IEventRepository eventRepo, IMediator mediator)
	{
		_participantRepo = participantRepo;
		_eventRepo = eventRepo;
		_mediator = mediator;
	}

	public async Task<CommandResponse> Handle(ConfirmBudgetRequest request, CancellationToken cancellationToken)
	{
		var ev = await _eventRepo.GetByEventIdAsync(request.EventId, cancellationToken);
		if (ev == null) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Event not found." };

		var participant = await _participantRepo.GetByTelegramUserIdAsync(request.EventId, request.TelegramUserId.ToString(), cancellationToken);
		if (participant == null) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Participant not found." };

		participant.PreferredGiftCategories = request.Accepted ? "Confirmed" : "Rejected";
		participant.UpdatedAtUtc = System.DateTime.UtcNow;
		await _participantRepo.UpdateAsync(participant, cancellationToken);

		var all = (await _participantRepo.GetAllByEventAsync(request.EventId, cancellationToken)).ToList();
		if (all.All(p => p.PreferredGiftCategories == "Confirmed"))
		{
			var transitionResponse = await _mediator.Send(new ApplyEventTransitionRequest
			{
				EventId = request.EventId,
				Trigger = EventWorkflowTrigger.BudgetConfirmedByAll,
				ChangedByTelegramId = request.TelegramUserId,
				Note = "Budget confirmed by all participants"
			}, cancellationToken);
			if (transitionResponse.StatusCode != System.Net.HttpStatusCode.OK)
			{
				return transitionResponse;
			}

			return new CommandResponse { StatusCode = System.Net.HttpStatusCode.OK, ResponseMessage = "Усі підтвердили бюджет. Жеребкування розпочнеться автоматично." };
		}

		return new CommandResponse { StatusCode = System.Net.HttpStatusCode.OK, ResponseMessage = "Ваш вибір зафіксовано." };
	}
}
