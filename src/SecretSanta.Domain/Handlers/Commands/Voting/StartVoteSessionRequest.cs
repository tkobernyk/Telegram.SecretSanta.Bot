using MediatR;
using SecretSanta.DAL.Entities;

namespace SecretSanta.Domain.Handlers.Commands.Voting;

public class StartVoteSessionRequest : IRequest<SecretSanta.Domain.Handlers.Common.CommandResponse>
{
	public string EventId { get; set; } = string.Empty;
	public long InitiatorTelegramId { get; set; }
	public VoteSession Session { get; set; } = new VoteSession();
}
