using MediatR;

namespace SecretSanta.Domain.Handlers.Commands.Voting;

public class ConfirmBudgetRequest : IRequest<SecretSanta.Domain.Handlers.Common.CommandResponse>
{
	public string EventId { get; set; } = string.Empty;
	public long TelegramUserId { get; set; }
	public bool Accepted { get; set; }
}
