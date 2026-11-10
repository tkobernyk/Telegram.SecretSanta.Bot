using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace SecretSanta.DAL.Cosmos;

public sealed class CosmosClientProvider
{
	readonly CosmosClient _client;
	readonly string _databaseName;

	public CosmosClientProvider(IConfiguration configuration)
	{
		var cosmosConfig = new CosmosConfiguration();
		configuration.GetSection(CosmosConfiguration.SectionName).Bind(cosmosConfig);

		var endpoint = cosmosConfig.Endpoint;
		var key = cosmosConfig.Key;
		_databaseName = cosmosConfig.DatabaseName;
		var applicationName = cosmosConfig.ApplicationName;

		if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
		{
			throw new ArgumentException("Cosmos__Endpoint and Cosmos__Key configuration values are required to create CosmosClient.");
		}

		if (string.IsNullOrEmpty(_databaseName))
		{
			throw new ArgumentException("Cosmos__DatabaseName configuration value is required to create CosmosClient.");
		}

		_client = new CosmosClient(endpoint, key, new CosmosClientOptions
		{
			ApplicationName = applicationName,
			AllowBulkExecution = false
		});
	}

	public CosmosClient Client => _client;

	public string DatabaseName => _databaseName;
}
