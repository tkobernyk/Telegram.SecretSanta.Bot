using Microsoft.Azure.Cosmos;

namespace SecretSanta.DAL.Abstractions;

public interface ICosmosClientFactory<TModel>
{
	Container GetContainer();
}
