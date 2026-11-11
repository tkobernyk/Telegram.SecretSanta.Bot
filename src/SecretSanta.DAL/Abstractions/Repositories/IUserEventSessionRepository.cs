using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Abstractions.Repositories;

public interface IUserEventSessionRepository
{
	Task<UserEventSession?> GetAsync(string telegramUserId, CancellationToken cancellationToken = default);
	Task UpsertAsync(UserEventSession session, CancellationToken cancellationToken = default);
	Task DeleteAsync(string telegramUserId, CancellationToken cancellationToken = default);
}
