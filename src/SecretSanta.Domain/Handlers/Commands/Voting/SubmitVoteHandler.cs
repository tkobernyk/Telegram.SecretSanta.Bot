using MediatR;
using SecretSanta.DAL.Abstractions.Repositories;
using SecretSanta.Domain.Handlers.Common;

namespace SecretSanta.Domain.Handlers.Commands.Voting;

public class SubmitVoteHandler : IRequestHandler<SubmitVoteRequest, CommandResponse>
{
	private readonly IVoteRepository _voteRepository;
	private readonly IParticipantRepository _participantRepository;

	public SubmitVoteHandler(IVoteRepository voteRepository, IParticipantRepository participantRepository)
	{
		_voteRepository = voteRepository;
		_participantRepository = participantRepository;
	}

	public async Task<CommandResponse> Handle(SubmitVoteRequest request, CancellationToken cancellationToken)
	{
		await _voteRepository.CreateVoteAsync(request.Vote, cancellationToken);
		var participant = await _participantRepository.GetByTelegramUserIdAsync(request.EventId, request.Vote.VoterTelegramId.ToString(), cancellationToken);
		if (participant != null)
		{
			participant.PreferredGiftCategories = request.Vote.OptionId;
			participant.RegistrationStatus = "Voted";
			participant.UpdatedAtUtc = DateTime.UtcNow;
			await _participantRepository.UpdateAsync(participant, cancellationToken);
		}

		return new CommandResponse { StatusCode = System.Net.HttpStatusCode.OK, ResponseMessage = "Дякуємо за голос." };
	}
}
