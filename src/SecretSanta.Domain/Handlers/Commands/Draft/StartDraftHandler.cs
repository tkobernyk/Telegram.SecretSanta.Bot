using MediatR;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Enums;
using SecretSanta.Domain.Handlers.Commands.Workflow;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.DAL.Abstractions.Repositories;

namespace SecretSanta.Domain.Handlers.Commands.Draft;

public class StartDraftHandler : IRequestHandler<StartDraftRequest, CommandResponse>
{
	readonly IDraftService _draftService;
	readonly IEventRepository _eventRepo;
	readonly IParticipantRepository _participantRepository;
	readonly IAssignmentRepository _assignmentRepository;
	readonly IMediator _mediator;

	public StartDraftHandler(IDraftService draftService, IEventRepository eventRepo, IParticipantRepository participantRepository, IAssignmentRepository assignmentRepository, IMediator mediator)
	{
		_draftService = draftService;
		_eventRepo = eventRepo;
		_participantRepository = participantRepository;
		_assignmentRepository = assignmentRepository;
		_mediator = mediator;
	}

	public async Task<CommandResponse> Handle(StartDraftRequest request, CancellationToken cancellationToken)
	{
		var ev = await _eventRepo.GetByEventIdAsync(request.EventId, cancellationToken);
		if (ev == null) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Event not found." };
		if (ev.OrganizerTelegramId != request.InitiatorTelegramId) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.Forbidden, ResponseMessage = "Only organizer may start draft." };

		var participants = (await _participantRepository.GetAllByEventAsync(request.EventId, cancellationToken)).ToList();
		var assignments = _draftService.BuildAssignments(request.EventId, participants.Select(participant => participant.TelegramUserId).ToList());
		await _assignmentRepository.CreateAssignmentsAsync(assignments, cancellationToken);

		var transitionResponse = await _mediator.Send(new ApplyEventTransitionRequest
		{
			EventId = request.EventId,
			Trigger = EventWorkflowTrigger.DraftStarted,
			ChangedByTelegramId = request.InitiatorTelegramId,
			Note = "Draft started"
		}, cancellationToken);
		if (transitionResponse.StatusCode != System.Net.HttpStatusCode.OK)
		{
			return transitionResponse;
		}

		return new CommandResponse { StatusCode = System.Net.HttpStatusCode.OK, ResponseMessage = "Жеребкування виконано та призначення опубліковано." };
	}
}
