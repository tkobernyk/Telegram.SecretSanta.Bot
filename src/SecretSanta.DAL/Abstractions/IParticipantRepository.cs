using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Abstractions;

public interface IParticipantRepository
{
	Task<Participant?> GetByTelegramUserIdAsync(string eventId, string telegramUserId, CancellationToken cancellationToken = default);
	Task CreateAsync(Participant participant, CancellationToken cancellationToken = default);
}
