using MediatR;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.DAL.Abstractions.Repositories;

namespace SecretSanta.Domain.Handlers.Commands.Voting;

public class AggregateVoteHandler : IRequestHandler<AggregateVoteRequest, CommandResponse>
{
	private readonly IVoteService _voteService;
	private readonly IEventRepository _eventRepo;
	private readonly IVoteRepository _voteRepository;
	private readonly IParticipantRepository _participantRepository;

	public AggregateVoteHandler(IVoteService voteService, IEventRepository eventRepo, IVoteRepository voteRepository, IParticipantRepository participantRepository)
	{
		_voteService = voteService;
		_eventRepo = eventRepo;
		_voteRepository = voteRepository;
		_participantRepository = participantRepository;
	}

	public async Task<CommandResponse> Handle(AggregateVoteRequest request, CancellationToken cancellationToken)
	{
		var ev = await _eventRepo.GetByEventIdAsync(request.EventId, cancellationToken);
		if (ev == null) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Event not found." };
		if (ev.OrganizerTelegramId != request.InitiatorTelegramId) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.Forbidden, ResponseMessage = "Only organizer may aggregate votes." };

		var votes = await _voteRepository.GetVotesForSessionAsync(request.EventId, request.VoteSessionId, cancellationToken);
		var participantsCount = (await _participantRepository.GetAllByEventAsync(request.EventId, cancellationToken)).Count();
		var outcome = _voteService.EvaluateBudgetVotes(votes, participantsCount);
		return new CommandResponse
		{
			StatusCode = outcome.TotalVotes == 0 ? System.Net.HttpStatusCode.BadRequest : System.Net.HttpStatusCode.OK,
			ResponseMessage = outcome.Message
		};
	}
}
