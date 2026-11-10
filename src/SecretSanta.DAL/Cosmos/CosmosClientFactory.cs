using System.Reflection;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using SecretSanta.DAL.Abstractions;

namespace SecretSanta.DAL.Cosmos;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CosmosContainerAttribute : Attribute
{
	public CosmosContainerAttribute(string name)
	{
		Name = name;
	}

	public string Name { get; }
}

public class CosmosClientFactory : ICosmosClientFactory
{
	readonly CosmosClient _client;
	readonly string _databaseName;
	readonly string _containerName;

	public CosmosClientFactory(IConfiguration configuration)
	{
		var cosmosConfig = new CosmosConfiguration();
		configuration.GetSection(CosmosConfiguration.SectionName).Bind(cosmosConfig);

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
		return GetContainer(_containerName);
	}

	public Container GetContainer(string containerName)
	{
		if (string.IsNullOrEmpty(_databaseName) || string.IsNullOrEmpty(containerName))
		{
			throw new ArgumentException("Cosmos__DatabaseName and container name are required to get a Cosmos Container.");
		}

		return _client.GetContainer(_databaseName, containerName);
	}

	public Container GetContainer<T>()
	{
		var type = typeof(T);
		var attribute = type.GetCustomAttribute<CosmosContainerAttribute>();
		var containerName = attribute?.Name ?? type.Name;
		return GetContainer(containerName);
	}
}