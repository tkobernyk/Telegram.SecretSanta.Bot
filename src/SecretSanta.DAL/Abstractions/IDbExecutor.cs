using Microsoft.Azure.Cosmos;

namespace SecretSanta.DAL.Abstractions;

public interface IDbExecutor<T>
{
	Task<T> GetItemByIdAsync(QueryDefinition query, string eventId, CancellationToken cancellationToken);
	Task<T> CreateItemAsync(T item, string eventId, CancellationToken cancellationToken);
	Task<T> UpsertItemAsync(T item, string eventId, CancellationToken cancellationToken);
	Task<IEnumerable<T>> QueryAsync(string query, object parameters, CancellationToken cancellationToken);
	Task DeleteItemAsync(string id, string partitionKey, CancellationToken cancellationToken);
}
