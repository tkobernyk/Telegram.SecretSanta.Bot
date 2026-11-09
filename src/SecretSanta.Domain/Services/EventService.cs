using System;
using System.Threading;
using System.Threading.Tasks;
using SecretSanta.Domain.Abstractions;
using SecretSanta.DAL.Abstractions;
using SecretSanta.Domain.Enums;

namespace SecretSanta.Domain.Services;

public class EventService : IEventService
{
	private readonly IEventRepository _eventRepo;

	public EventService(IEventRepository eventRepo)
	{
		_eventRepo = eventRepo;
	}

	public async Task<EventState> GetEventStateAsync(string eventId, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(eventId)) return EventState.Registration;
		var ev = await _eventRepo.GetByEventIdAsync(eventId, cancellationToken);
		if (ev == null) return EventState.Registration;

		if (Enum.TryParse<EventState>(ev.State, ignoreCase: true, out var state))
		{
			return state;
		}

		return EventState.Registration;
	}
}
