using MediatR;

namespace SecretSanta.Domain.Handlers.Commands.Voting;

public class AggregateVoteRequest : IRequest<SecretSanta.Domain.Handlers.Common.CommandResponse>
{
	public string EventId { get; set; } = string.Empty;
	public string VoteSessionId { get; set; } = string.Empty;
	public long InitiatorTelegramId { get; set; }
}
