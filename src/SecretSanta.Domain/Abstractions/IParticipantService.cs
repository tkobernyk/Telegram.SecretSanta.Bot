using SecretSanta.Domain.Models;

namespace SecretSanta.Domain.Abstractions;

public interface IParticipantService
{
	Task<ParticipantDto?> GetByTelegramUserIdAsync(string eventId, string telegramUserId, CancellationToken cancellationToken = default);
	Task<ParticipantDto> CreateAsync(ParticipantDto participant, CancellationToken cancellationToken = default);
}
