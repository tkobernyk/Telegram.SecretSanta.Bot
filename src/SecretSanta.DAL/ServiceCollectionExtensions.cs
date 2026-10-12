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
		services.AddSingleton<ICosmosClientFactory, CosmosClientFactory>();

		services.AddSingleton<IExecutor, CosmosExecutor>();
		services.AddSingleton<IParticipantRepository>(sp => sp.GetRequiredService<CosmosParticipantRepository>());
		return services;
	}
}
