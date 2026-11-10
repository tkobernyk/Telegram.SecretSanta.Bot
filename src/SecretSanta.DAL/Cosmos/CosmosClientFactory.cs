using Microsoft.Azure.Cosmos;
using SecretSanta.DAL.Abstractions;

namespace SecretSanta.DAL.Cosmos;

public sealed class CosmosClientFactory<TModel> : ICosmosClientFactory<TModel>
{
	readonly CosmosClientProvider _provider;

	public CosmosClientFactory(CosmosClientProvider provider)
	{
		_provider = provider;
	}

	public Container GetContainer()
	{
		var containerName = $"{nameof(TModel)}s";
		if (string.IsNullOrWhiteSpace(containerName))
		{
			throw new ArgumentException($"Unable to resolve Cosmos container for model type '{nameof(TModel)}'.");
		}

		return _provider.Client.GetContainer(_provider.DatabaseName, containerName);
	}
}
