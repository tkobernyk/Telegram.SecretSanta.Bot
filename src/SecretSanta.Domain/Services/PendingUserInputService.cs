using System.Collections.Concurrent;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Models;
using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;

namespace SecretSanta.Domain.Abstractions;

public class PendingUserInputService : IPendingUserInputService
{
	readonly IUserEventSessionRepository? _userEventSessionRepository;
	readonly ConcurrentDictionary<string, ConversationState> _dict = new();
	readonly ConcurrentDictionary<string, string> _activeEventIds = new();

	public PendingUserInputService(IUserEventSessionRepository? userEventSessionRepository = null)
	{
		_userEventSessionRepository = userEventSessionRepository;
	}

	public void StartConversation(string telegramUserId, ConversationState state)
	{
		_dict[telegramUserId] = state;
	}

	public bool TryGetConversation(string telegramUserId, out ConversationState? state)
	{
		if (_dict.TryGetValue(telegramUserId, out var currentState))
		{
			state = currentState;
			return true;
		}

		state = null;
		return false;
	}

	public void UpdateConversation(string telegramUserId, ConversationState state)
	{
		_dict[telegramUserId] = state;
	}

	public void EndConversation(string telegramUserId)
	{
		_dict.TryRemove(telegramUserId, out _);
	}

	public void SetActiveEventId(string telegramUserId, string eventId)
	{
		if (!string.IsNullOrWhiteSpace(telegramUserId) && !string.IsNullOrWhiteSpace(eventId))
		{
			_activeEventIds[telegramUserId] = eventId;
		}
	}

	public bool TryGetActiveEventId(string telegramUserId, out string eventId)
	{
		return _activeEventIds.TryGetValue(telegramUserId, out eventId!);
	}

	public void ClearActiveEventId(string telegramUserId)
	{
		_activeEventIds.TryRemove(telegramUserId, out _);
	}

	public async Task SetActiveEventIdAsync(string telegramUserId, string eventId, CancellationToken cancellationToken = default)
	{
		SetActiveEventId(telegramUserId, eventId);
		if (_userEventSessionRepository != null)
		{
			await _userEventSessionRepository.UpsertAsync(new UserEventSession { TelegramUserId = telegramUserId, EventId = eventId }, cancellationToken);
		}
	}

	public async Task<string?> GetActiveEventIdAsync(string telegramUserId, CancellationToken cancellationToken = default)
	{
		if (_activeEventIds.TryGetValue(telegramUserId, out var eventId)) return eventId;
		if (_userEventSessionRepository == null) return null;

		var session = await _userEventSessionRepository.GetAsync(telegramUserId, cancellationToken);
		if (session == null) return null;
		_activeEventIds[telegramUserId] = session.EventId;
		return session.EventId;
	}

	public async Task ClearActiveEventIdAsync(string telegramUserId, CancellationToken cancellationToken = default)
	{
		ClearActiveEventId(telegramUserId);
		if (_userEventSessionRepository != null)
		{
			await _userEventSessionRepository.DeleteAsync(telegramUserId, cancellationToken);
		}
	}
}
