using MediatR;
using SecretSanta.Domain.Enums;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.DAL.Abstractions;

namespace SecretSanta.Domain.Handlers.Commands.Voting;

public class ConfirmBudgetHandler : IRequestHandler<ConfirmBudgetRequest, CommandResponse>
{
	private readonly IParticipantRepository _participantRepo;
	private readonly IEventRepository _eventRepo;

	public ConfirmBudgetHandler(IParticipantRepository participantRepo, IEventRepository eventRepo)
	{
		_participantRepo = participantRepo;
		_eventRepo = eventRepo;
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
			// everyone confirmed - start draft automatically
			// NOTE: orchestration to start draft is handled by separate command/handler; here just set event flag and return instruction
			ev.State = EventWorkflowState.Draft;
			ev.UpdatedAtUtc = System.DateTime.UtcNow;
			await _eventRepo.UpdateAsync(ev, cancellationToken);

			return new CommandResponse { StatusCode = System.Net.HttpStatusCode.OK, ResponseMessage = "Усі підтвердили бюджет. Жеребкування розпочнеться автоматично." };
		}

		return new CommandResponse { StatusCode = System.Net.HttpStatusCode.OK, ResponseMessage = "Ваш вибір зафіксовано." };
	}
}
