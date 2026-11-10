using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using SecretSanta.DAL.Abstractions;

namespace SecretSanta.DAL.Cosmos;

public class CosmosExecutor<T>(ICosmosClientFactory<T> factory, ILogger<CosmosExecutor<T>> logger) : IDbExecutor<T>
{
	readonly Container _container = factory.GetContainer();
	readonly ILogger<CosmosExecutor<T>> _logger = logger;


	public async Task<T> GetItemByIdAsync(QueryDefinition query, string eventId, CancellationToken cancellationToken)
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

	public async Task<T> CreateItemAsync(T item, string eventId, CancellationToken cancellationToken)
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

	public async Task<T> UpsertItemAsync(T item, string eventId, CancellationToken cancellationToken)
	{
		try
		{
			var response = await _container.UpsertItemAsync(item, new PartitionKey(eventId), cancellationToken: cancellationToken);
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

	public async Task<IEnumerable<T>> QueryAsync(string query, object parameters, CancellationToken cancellationToken)
	{
		var sql = new QueryDefinition(query);
		if (parameters is not null)
		{
			var props = parameters.GetType().GetProperties();
			foreach (var prop in props)
			{
				var value = prop.GetValue(parameters);
				if (value is null) continue;
				sql = sql.WithParameter($"@{prop.Name}", value);
			}
		}

		using var iterator = _container.GetItemQueryIterator<T>(sql);
		var results = new List<T>();
		while (iterator.HasMoreResults)
		{
			var page = await iterator.ReadNextAsync(cancellationToken);
			results.AddRange(page.Resource);
		}
		return results;
	}

	public async Task DeleteItemAsync(string id, string partitionKey, CancellationToken cancellationToken)
	{
		await _container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey), cancellationToken: cancellationToken);
	}
}
