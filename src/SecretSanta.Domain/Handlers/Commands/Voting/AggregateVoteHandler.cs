using MediatR;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.DAL.Abstractions;

namespace SecretSanta.Domain.Handlers.Commands.Voting;

public class AggregateVoteHandler : IRequestHandler<AggregateVoteRequest, CommandResponse>
{
	private readonly IVoteService _voteService;
	private readonly IEventRepository _eventRepo;

	public AggregateVoteHandler(IVoteService voteService, IEventRepository eventRepo)
	{
		_voteService = voteService;
		_eventRepo = eventRepo;
	}

	public async Task<CommandResponse> Handle(AggregateVoteRequest request, CancellationToken cancellationToken)
	{
		var ev = await _eventRepo.GetByEventIdAsync(request.EventId, cancellationToken);
		if (ev == null) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Event not found." };
		if (ev.OrganizerTelegramId != request.InitiatorTelegramId) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.Forbidden, ResponseMessage = "Only organizer may aggregate votes." };

		var outcome = await _voteService.EvaluateBudgetVotesAsync(request.EventId, request.VoteSessionId, cancellationToken);
		return new CommandResponse
		{
			StatusCode = outcome.TotalVotes == 0 ? System.Net.HttpStatusCode.BadRequest : System.Net.HttpStatusCode.OK,
			ResponseMessage = outcome.Message
		};
	}
}
