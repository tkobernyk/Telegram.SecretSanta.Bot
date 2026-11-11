using System.Collections.Concurrent;
using SecretSanta.DAL.Abstractions.Repositories;
using SecretSanta.DAL.Entities;

namespace SecretSanta.Domain.Tests;

internal sealed class FakeUserEventSessionRepository : IUserEventSessionRepository
{
	private readonly ConcurrentDictionary<string, UserEventSession> _store = new();

	public Task<UserEventSession?> GetAsync(string telegramUserId, CancellationToken cancellationToken = default)
	{
		_store.TryGetValue(telegramUserId, out var session);
		return Task.FromResult(session);
	}

	public Task UpsertAsync(UserEventSession session, CancellationToken cancellationToken = default)
	{
		_store[session.TelegramUserId] = session;
		return Task.CompletedTask;
	}

	public Task DeleteAsync(string telegramUserId, CancellationToken cancellationToken = default)
	{
		_store.TryRemove(telegramUserId, out _);
		return Task.CompletedTask;
	}
}