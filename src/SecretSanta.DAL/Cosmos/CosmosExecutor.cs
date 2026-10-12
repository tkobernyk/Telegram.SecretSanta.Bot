using Microsoft.Azure.Cosmos;
using SecretSanta.DAL.Abstractions;

namespace SecretSanta.DAL.Cosmos;

public class CosmosExecutor(ICosmosClientFactory factory) : IExecutor
{
	readonly Container _container = factory.GetContainer();

	public async Task<T> GetItemByIdAsync<T>(QueryDefinition query, string eventId, CancellationToken cancellationToken)
	{
		using var it = _container.GetItemQueryIterator<T>(query,
			requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(eventId), MaxItemCount = 1 });

		if (it.HasMoreResults)
		{
			var res = await it.ReadNextAsync(cancellationToken);
			var item = res.Resource.FirstOrDefault()!;
			return item;
		}
		return default!;
	}

	public async Task<T> CreateItemAsync<T>(T item, string eventId, CancellationToken cancellationToken)
	{
		var response = await _container.CreateItemAsync(item, new PartitionKey(eventId), cancellationToken: cancellationToken);
		return response.Resource;
	}
}
