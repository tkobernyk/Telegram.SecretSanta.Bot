using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using SecretSanta.DAL.Abstractions;

namespace SecretSanta.DAL.Cosmos;

public class CosmosExecutor(ICosmosClientFactory factory, ILogger<CosmosExecutor> logger) : IExecutor
{
	readonly Container _container = factory.GetContainer();
	readonly ILogger<CosmosExecutor> _logger = logger;

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
		try
		{
			var response = await _container.CreateItemAsync(item, new PartitionKey(eventId), cancellationToken: cancellationToken);
			return response.Resource;
		}
		catch (CosmosException ex)
		{
			_logger.LogError(
				ex,
				"Cosmos DB failed. Status={StatusCode}, " +
				"SubStatus={SubStatusCode}, Message={Message}, " +
				"ActivityId={ActivityId}",
				(int)ex.StatusCode,
				ex.SubStatusCode,
				ex.Message,
				ex.ActivityId);

			throw;
		}
	}
}
