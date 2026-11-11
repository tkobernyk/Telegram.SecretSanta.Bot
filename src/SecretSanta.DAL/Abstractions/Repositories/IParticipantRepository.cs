using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Abstractions.Repositories;

public interface IParticipantRepository
{
	Task<Participant?> GetByTelegramUserIdAsync(string eventId, string telegramUserId, CancellationToken cancellationToken = default);
	Task CreateAsync(Participant participant, CancellationToken cancellationToken = default);
	Task UpdateAsync(Participant participant, CancellationToken cancellationToken = default);
	Task<IEnumerable<Participant>> GetAllByEventAsync(string eventId, CancellationToken cancellationToken = default);
}
