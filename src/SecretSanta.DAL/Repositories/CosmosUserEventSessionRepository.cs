using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Repositories;

public sealed class CosmosUserEventSessionRepository(IExecutor executor) : IUserEventSessionRepository
{
	const string Container = "UserEventSessions";
	readonly IExecutor _executor = executor;

	public async Task<UserEventSession?> GetAsync(string telegramUserId, CancellationToken cancellationToken = default)
	{
		var sessions = await _executor.QueryAsync<UserEventSession>(Container,
			"SELECT * FROM c WHERE c.telegramUserId = @telegramUserId",
			new { telegramUserId },
			cancellationToken);
		return sessions.FirstOrDefault();
	}

	public async Task UpsertAsync(UserEventSession session, CancellationToken cancellationToken = default)
	{
		session.Id = session.TelegramUserId;
		session.UpdatedAtUtc = DateTime.UtcNow;
		await _executor.UpsertItemAsync(Container, session.TelegramUserId, session, cancellationToken);
	}

	public Task DeleteAsync(string telegramUserId, CancellationToken cancellationToken = default)
	{
		return _executor.DeleteItemAsync<UserEventSession>(Container, telegramUserId, telegramUserId, cancellationToken);
	}
}
