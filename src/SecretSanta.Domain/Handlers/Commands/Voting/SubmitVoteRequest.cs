using MediatR;
using SecretSanta.DAL.Entities;

namespace SecretSanta.Domain.Handlers.Commands.Voting;

public class SubmitVoteRequest : IRequest<SecretSanta.Domain.Handlers.Common.CommandResponse>
{
	public string EventId { get; set; } = string.Empty;
	public string VoteSessionId { get; set; } = string.Empty;
	public Vote Vote { get; set; } = new Vote();
}
