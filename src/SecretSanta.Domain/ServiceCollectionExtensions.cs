using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecretSanta.Infrastructure;

namespace SecretSanta.Domain;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddSecretSantaDomain(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSecretSantaInfrastructure(configuration);
		services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));
		return services;
	}
}
