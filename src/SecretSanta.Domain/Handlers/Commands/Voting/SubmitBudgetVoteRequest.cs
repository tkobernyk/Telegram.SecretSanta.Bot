using MediatR;
using SecretSanta.Domain.Handlers.Common;

namespace SecretSanta.Domain.Handlers.Commands.Voting;

public sealed class SubmitBudgetVoteRequest : IRequest<CommandResponse>
{
	public string EventId { get; set; } = string.Empty;
	public long TelegramUserId { get; set; }
	public long Budget { get; set; }
}
