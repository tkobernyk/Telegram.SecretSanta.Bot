using MediatR;
using SecretSanta.DAL.Abstractions.Repositories;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.Domain.Models.Workflow;

namespace SecretSanta.Domain.Handlers.Commands.Workflow;

public sealed class ApplyEventTransitionHandler : IRequestHandler<ApplyEventTransitionRequest, CommandResponse>
{
	readonly IEventRepository _eventRepository;
	readonly IEventStateMachine _stateMachine;

	public ApplyEventTransitionHandler(IEventRepository eventRepository, IEventStateMachine stateMachine)
	{
		_eventRepository = eventRepository;
		_stateMachine = stateMachine;
	}

	public async Task<CommandResponse> Handle(ApplyEventTransitionRequest request, CancellationToken cancellationToken)
	{
		var ev = await _eventRepository.GetByEventIdAsync(request.EventId, cancellationToken);
		if (ev == null)
		{
			return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Event not found." };
		}

		var transition = _stateMachine.Transition(ev, request.Trigger, new EventStateTransitionContext
		{
			ChangedByTelegramId = request.ChangedByTelegramId,
			Note = request.Note
		});

		if (!string.IsNullOrWhiteSpace(transition.Error))
		{
			return new CommandResponse { StatusCode = System.Net.HttpStatusCode.Conflict, ResponseMessage = transition.Error };
		}

		await _eventRepository.UpdateAsync(ev, cancellationToken);
		return new CommandResponse { StatusCode = System.Net.HttpStatusCode.OK };
	}
}