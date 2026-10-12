using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Cosmos;
using SecretSanta.DAL.Repositories;

namespace SecretSanta.DAL;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddSecretSantaDal(this IServiceCollection services, IConfiguration configuration)
	{
		// Register concrete types so tests and consumers can resolve either the concrete implementation or the abstraction.
		services.AddSingleton<CosmosClientFactory>();
		services.AddSingleton<ICosmosClientFactory>(sp => sp.GetRequiredService<CosmosClientFactory>());

		services.AddSingleton<CosmosExecutor>();
		services.AddSingleton<IExecutor>(sp => sp.GetRequiredService<CosmosExecutor>());

		services.AddSingleton<CosmosParticipantRepository>();
		services.AddSingleton<IParticipantRepository>(sp => sp.GetRequiredService<CosmosParticipantRepository>());
		return services;
	}
}
