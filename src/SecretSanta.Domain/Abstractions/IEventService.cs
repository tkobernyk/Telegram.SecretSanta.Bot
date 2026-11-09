using SecretSanta.Domain.Enums;

namespace SecretSanta.Domain.Abstractions;

public interface IEventService
{
	Task<EventState> GetEventStateAsync(string eventId, CancellationToken cancellationToken = default);
}
