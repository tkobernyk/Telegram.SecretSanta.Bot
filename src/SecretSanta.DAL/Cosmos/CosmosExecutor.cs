using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using SecretSanta.DAL.Abstractions;

namespace SecretSanta.DAL.Cosmos;

public class CosmosExecutor(ICosmosClientFactory factory, ILogger<CosmosExecutor> logger) : IExecutor
{
	readonly ICosmosClientFactory _factory = factory;
	readonly Container _container = factory.GetContainer();
	readonly ILogger<CosmosExecutor> _logger = logger;

	private Container ResolveContainer(string? containerName = null)
	{
		return string.IsNullOrWhiteSpace(containerName) ? _container : _factory.GetContainer(containerName);
	}

	public async Task<T> GetItemByIdAsync<T>(QueryDefinition query, string eventId, CancellationToken cancellationToken)
	{
		using var it = ResolveContainer().GetItemQueryIterator<T>(query,
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
			var response = await ResolveContainer().CreateItemAsync(item, new PartitionKey(eventId), cancellationToken: cancellationToken);
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

	public async Task<T> UpsertItemAsync<T>(T item, string eventId, CancellationToken cancellationToken)
	{
		try
		{
			var response = await ResolveContainer().UpsertItemAsync(item, new PartitionKey(eventId), cancellationToken: cancellationToken);
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

	public async Task<IEnumerable<T>> QueryAsync<T>(string containerName, string query, object parameters, CancellationToken cancellationToken)
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

		using var iterator = ResolveContainer(containerName).GetItemQueryIterator<T>(sql);
		var results = new List<T>();
		while (iterator.HasMoreResults)
		{
			var page = await iterator.ReadNextAsync(cancellationToken);
			results.AddRange(page.Resource);
		}
		return results;
	}

	public Task<T> CreateItemAsync<T>(string containerName, string eventId, T item, CancellationToken cancellationToken)
	{
		try
		{
			var container = ResolveContainer(containerName);
			return CreateItemInContainerAsync(container, item, eventId, cancellationToken);
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

	public Task<T> UpsertItemAsync<T>(string containerName, string eventId, T item, CancellationToken cancellationToken)
	{
		try
		{
			var container = ResolveContainer(containerName);
			return UpsertItemInContainerAsync(container, item, eventId, cancellationToken);
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

	private async Task<T> CreateItemInContainerAsync<T>(Container container, T item, string eventId, CancellationToken cancellationToken)
	{
		var response = await container.CreateItemAsync(item, new PartitionKey(eventId), cancellationToken: cancellationToken);
		return response.Resource;
	}

	private async Task<T> UpsertItemInContainerAsync<T>(Container container, T item, string eventId, CancellationToken cancellationToken)
	{
		var response = await container.UpsertItemAsync(item, new PartitionKey(eventId), cancellationToken: cancellationToken);
		return response.Resource;
	}

	public async Task DeleteItemAsync<T>(string containerName, string id, string partitionKey, CancellationToken cancellationToken)
	{
		await ResolveContainer(containerName).DeleteItemAsync<T>(id, new PartitionKey(partitionKey), cancellationToken: cancellationToken);
	}
}
