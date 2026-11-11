using SecretSanta.DAL.Abstractions.Repositories;
using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.Domain.Models;

namespace SecretSanta.Domain.Tests;

internal sealed class PendingUserInputService : IUserEventSessionRepository
{
	private readonly Dictionary<string, UserEventSession> _sessions = new();

	public void StartConversation(string telegramUserId, ConversationState state) => InteractionStateStore.StartConversation(telegramUserId, state);
	public bool TryGetConversation(string telegramUserId, out ConversationState? state) => InteractionStateStore.TryGetConversation(telegramUserId, out state);
	public void UpdateConversation(string telegramUserId, ConversationState state) => InteractionStateStore.UpdateConversation(telegramUserId, state);
	public void EndConversation(string telegramUserId) => InteractionStateStore.EndConversation(telegramUserId);
	public void SetActiveEventId(string telegramUserId, string eventId) => InteractionStateStore.SetActiveEventId(telegramUserId, eventId);
	public bool TryGetActiveEventId(string telegramUserId, out string eventId) => InteractionStateStore.TryGetActiveEventId(telegramUserId, out eventId);
	public void ClearActiveEventId(string telegramUserId) => InteractionStateStore.ClearActiveEventId(telegramUserId);

	public async Task SetActiveEventIdAsync(string telegramUserId, string eventId, CancellationToken cancellationToken = default)
	{
		SetActiveEventId(telegramUserId, eventId);
		await UpsertAsync(new UserEventSession { TelegramUserId = telegramUserId, EventId = eventId }, cancellationToken);
	}

	public async Task<string?> GetActiveEventIdAsync(string telegramUserId, CancellationToken cancellationToken = default)
	{
		if (TryGetActiveEventId(telegramUserId, out var eventId)) return eventId;
		var session = await GetAsync(telegramUserId, cancellationToken);
		if (session == null) return null;
		SetActiveEventId(telegramUserId, session.EventId);
		return session.EventId;
	}

	public async Task ClearActiveEventIdAsync(string telegramUserId, CancellationToken cancellationToken = default)
	{
		ClearActiveEventId(telegramUserId);
		await DeleteAsync(telegramUserId, cancellationToken);
	}

	public Task<UserEventSession?> GetAsync(string telegramUserId, CancellationToken cancellationToken = default)
	{
		_sessions.TryGetValue(telegramUserId, out var session);
		return Task.FromResult(session);
	}

	public Task UpsertAsync(UserEventSession session, CancellationToken cancellationToken = default)
	{
		_sessions[session.TelegramUserId] = session;
		return Task.CompletedTask;
	}

	public Task DeleteAsync(string telegramUserId, CancellationToken cancellationToken = default)
	{
		_sessions.Remove(telegramUserId);
		return Task.CompletedTask;
	}
}