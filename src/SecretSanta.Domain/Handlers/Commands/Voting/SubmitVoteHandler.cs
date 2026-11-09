using MediatR;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Handlers.Common;

namespace SecretSanta.Domain.Handlers.Commands.Voting;

public class SubmitVoteHandler : IRequestHandler<SubmitVoteRequest, CommandResponse>
{
	private readonly IVoteService _voteService;

	public SubmitVoteHandler(IVoteService voteService)
	{
		_voteService = voteService;
	}

	public async Task<CommandResponse> Handle(SubmitVoteRequest request, CancellationToken cancellationToken)
	{
		await _voteService.SubmitVoteAsync(request.EventId, request.VoteSessionId, request.Vote, cancellationToken);
		return new CommandResponse { StatusCode = System.Net.HttpStatusCode.OK, ResponseMessage = "Дякуємо за голос." };
	}
}
