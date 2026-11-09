using SecretSanta.DAL.Abstractions;
using SecretSanta.Domain.Abstractions;

namespace SecretSanta.Domain.Services;

public class InMemoryAssignmentService : IAssignmentService
{
    private readonly IAssignmentRepository _assignmentRepository;

    public InMemoryAssignmentService(IAssignmentRepository assignmentRepository)
    {
        _assignmentRepository = assignmentRepository;
    }

    public async Task<long?> GetSantaForRecipientAsync(string eventId, long recipientTelegramUserId, CancellationToken cancellationToken = default)
    {
        var assignment = await _assignmentRepository.GetAssignmentForRecipientAsync(eventId, recipientTelegramUserId, cancellationToken);
        return assignment == null ? null : assignment.SantaTelegramUserId;
    }

    public async Task<long?> GetRecipientForSantaAsync(string eventId, long santaTelegramUserId, CancellationToken cancellationToken = default)
    {
        var assignment = await _assignmentRepository.GetAssignmentForSantaAsync(eventId, santaTelegramUserId, cancellationToken);
        return assignment == null ? null : assignment.RecipientTelegramUserId;
    }
}

