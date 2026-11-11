using SecretSanta.DAL.Entities;

namespace SecretSanta.Domain.Abstractions;

public interface IDraftService
{
	IReadOnlyList<Assignment> BuildAssignments(string eventId, IReadOnlyList<string> participantTelegramUserIds);
}
