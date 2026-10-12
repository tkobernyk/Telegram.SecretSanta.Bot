using Microsoft.Azure.Cosmos;

namespace SecretSanta.DAL.Abstractions;

public interface ICosmosClientFactory
{
	Container GetContainer();
}
