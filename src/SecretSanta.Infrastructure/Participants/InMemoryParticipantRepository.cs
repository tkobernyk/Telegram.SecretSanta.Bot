using System.Collections.Concurrent;
using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;

namespace SecretSanta.Infrastructure.Participants;

public class InMemoryParticipantRepository : IParticipantRepository
{
	readonly ConcurrentDictionary<string, Participant> _store = new ();

	public Task<Participant?> GetByTelegramUserIdAsync(string eventId, string telegramUserId, CancellationToken cancellationToken = default)
	{
		var p = _store.Values.FirstOrDefault(x => x.EventId == eventId && x.TelegramUserId == telegramUserId);
		return Task.FromResult(p);
	}

	public Task CreateAsync(Participant participant, CancellationToken cancellationToken = default)
	{
		_store[participant.Id] = participant;
		return Task.CompletedTask;
	}
}
