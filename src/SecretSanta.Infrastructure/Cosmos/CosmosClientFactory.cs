using Microsoft.Extensions.Configuration;

namespace SecretSanta.Infrastructure.Cosmos;

public class CosmosClientFactory
{
	readonly string _endpoint;
	readonly string _key;

	public CosmosClientFactory(IConfiguration configuration)
	{
		_endpoint = configuration["Cosmos__Endpoint"] ?? string.Empty;
		_key = configuration["Cosmos__Key"] ?? string.Empty;

		if (string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_key))
		{
			// Do not throw here; keep as a placeholder for local development.
		}
	}

	public object? Client => null;
}
