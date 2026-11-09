namespace SecretSanta.Domain.Abstractions;

public interface IAssignmentService
{
	Task<long?> GetSantaForRecipientAsync(string eventId, long recipientTelegramUserId, CancellationToken cancellationToken = default);
	Task<long?> GetRecipientForSantaAsync(string eventId, long santaTelegramUserId, CancellationToken cancellationToken = default);
}
