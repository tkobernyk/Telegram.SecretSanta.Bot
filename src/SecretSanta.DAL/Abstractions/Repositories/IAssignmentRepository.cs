using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Abstractions.Repositories;

public interface IAssignmentRepository
{
	Task CreateAssignmentsAsync(IEnumerable<Assignment> assignments, CancellationToken cancellationToken = default);
	Task<IEnumerable<Assignment>> GetAssignmentsByEventAsync(string eventId, CancellationToken cancellationToken = default);
	Task<Assignment?> GetAssignmentForRecipientAsync(string eventId, long recipientTelegramId, CancellationToken cancellationToken = default);
	Task<Assignment?> GetAssignmentForSantaAsync(string eventId, long santaTelegramId, CancellationToken cancellationToken = default);
}
