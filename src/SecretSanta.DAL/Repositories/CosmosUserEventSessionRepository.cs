using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Abstractions.Repositories;
using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Repositories;

public sealed class CosmosUserEventSessionRepository(IDbExecutor<UserEventSession> executor) : IUserEventSessionRepository
{
	readonly IDbExecutor<UserEventSession> _executor = executor;

	public async Task<UserEventSession?> GetAsync(string telegramUserId, CancellationToken cancellationToken = default)
	{
		var sessions = await _executor.QueryAsync("SELECT * FROM c WHERE c.telegramUserId = @telegramUserId",
			new { telegramUserId },
			cancellationToken);
		return sessions.FirstOrDefault();
	}

	public async Task UpsertAsync(UserEventSession session, CancellationToken cancellationToken = default)
	{
		session.Id = session.TelegramUserId;
		session.UpdatedAtUtc = DateTime.UtcNow;
		await _executor.UpsertItemAsync(session, session.TelegramUserId, cancellationToken);
	}

	public Task DeleteAsync(string telegramUserId, CancellationToken cancellationToken = default)
	{
		return _executor.DeleteItemAsync(telegramUserId, telegramUserId, cancellationToken);
	}
}
