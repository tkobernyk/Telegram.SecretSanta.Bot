using Microsoft.Azure.Cosmos;

namespace SecretSanta.DAL.Abstractions;

public interface IExecutor
{
	Task<T> GetItemByIdAsync<T>(QueryDefinition query, string eventId, CancellationToken cancellationToken);
	Task<T> CreateItemAsync<T>(T item, string eventId, CancellationToken cancellationToken);
}
