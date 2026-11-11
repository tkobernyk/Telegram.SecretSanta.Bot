using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Abstractions.Repositories;

public interface IEventRepository
{
	Task CreateAsync(Event ev, CancellationToken cancellationToken = default);
	Task<Event?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken = default);
	Task<Event?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);
	Task UpdateAsync(Event ev, CancellationToken cancellationToken = default);
	Task<IEnumerable<Event>> QueryByStateAsync(string state, CancellationToken cancellationToken = default);
}
