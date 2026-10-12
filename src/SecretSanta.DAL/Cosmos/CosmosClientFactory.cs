using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using SecretSanta.DAL.Abstractions;

namespace SecretSanta.DAL.Cosmos;

public class CosmosClientFactory : ICosmosClientFactory
{
	readonly CosmosClient _client;
	readonly string _databaseName;
	readonly string _containerName;

	public CosmosClientFactory(IConfiguration configuration)
	{
		var cosmosConfig = new CosmosConfiguration();
		configuration.GetSection("Cosmos").Bind(cosmosConfig);

		var endpoint = cosmosConfig.Endpoint;
		var key = cosmosConfig.Key;
		_databaseName = cosmosConfig.DatabaseName;
		_containerName = cosmosConfig.ContainerName;
		var applicationName = cosmosConfig.ApplicationName;

		if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
		{
			throw new ArgumentException("Cosmos__Endpoint and Cosmos__Key configuration values are required to create CosmosClient.");
		}

		_client = new CosmosClient(endpoint, key, new CosmosClientOptions
		{
			ApplicationName = applicationName,
			AllowBulkExecution = false
		});
	}

	public CosmosClient Client => _client;

	public Container GetContainer()
	{
		if (string.IsNullOrEmpty(_databaseName) || string.IsNullOrEmpty(_containerName))
		{
			throw new ArgumentException("Cosmos__DatabaseName and Cosmos__ContainerName configuration values are required to get a Cosmos Container.");
		}
		return _client.GetContainer(_databaseName, _containerName);
	}
}
