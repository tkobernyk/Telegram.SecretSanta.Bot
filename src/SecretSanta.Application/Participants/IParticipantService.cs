using SecretSanta.Domain.Entities;

namespace SecretSanta.Application.Participants
{
	public interface IParticipantService
	{
		Task<Participant?> GetByTelegramUserIdAsync(string eventId, string telegramUserId, CancellationToken cancellationToken = default);
		Task<Participant> CreateAsync(Participant participant, CancellationToken cancellationToken = default);
	}
}
