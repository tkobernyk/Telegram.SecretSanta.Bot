using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace SecretSanta.DAL.Cosmos;

public sealed class CosmosClientProvider
{
	readonly CosmosClient _client;
	readonly string _databaseName;

	public CosmosClientProvider(IConfiguration configuration)
	{
		string GetSetting(string colonKey, string doubleUnderscoreKey)
			=> configuration[colonKey]
				?? configuration[doubleUnderscoreKey]
				?? configuration[$"{doubleUnderscoreKey}"]
				?? string.Empty;

		var endpoint = GetSetting("Cosmos:Endpoint", "Cosmos__Endpoint");
		var key = GetSetting("Cosmos:Key", "Cosmos__Key");
		_databaseName = GetSetting("Cosmos:DatabaseName", "Cosmos__DatabaseName");
		var applicationName = GetSetting("Cosmos:ApplicationName", "Cosmos__ApplicationName");
		if (string.IsNullOrWhiteSpace(applicationName))
		{
			applicationName = "SecretSanta";
		}

		if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
		{
			throw new ArgumentException("Cosmos:Endpoint and Cosmos:Key configuration values are required to create CosmosClient.");
		}

		if (endpoint.StartsWith("<", StringComparison.Ordinal) || key.StartsWith("<", StringComparison.Ordinal))
		{
			throw new ArgumentException("Cosmos configuration contains placeholder values. Set real Cosmos__Endpoint and Cosmos__Key.");
		}

		if (string.IsNullOrWhiteSpace(_databaseName))
		{
			throw new ArgumentException("Cosmos__DatabaseName configuration value is required to create CosmosClient.");
		}

		try
		{
			Convert.FromBase64String(key);
		}
		catch (FormatException ex)
		{
			throw new ArgumentException("Cosmos__Key is not a valid Base64 Cosmos DB key.", ex);
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
