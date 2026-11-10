using Microsoft.Azure.Cosmos;

namespace SecretSanta.DAL.Abstractions;

public interface IExecutor
{
	Task<T> GetItemByIdAsync<T>(QueryDefinition query, string eventId, CancellationToken cancellationToken);
	Task<T> CreateItemAsync<T>(T item, string eventId, CancellationToken cancellationToken);
	Task<T> UpsertItemAsync<T>(T item, string eventId, CancellationToken cancellationToken);
	Task<IEnumerable<T>> QueryAsync<T>(string containerName, string query, object parameters, CancellationToken cancellationToken);
	Task<T> CreateItemAsync<T>(string containerName, string eventId, T item, CancellationToken cancellationToken);
	Task<T> UpsertItemAsync<T>(string containerName, string eventId, T item, CancellationToken cancellationToken);
	Task DeleteItemAsync<T>(string containerName, string id, string partitionKey, CancellationToken cancellationToken);
}
