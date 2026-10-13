using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Participants;
using SecretSanta.Infrastructure;

namespace SecretSanta.Domain;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddSecretSantaDomain(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSecretSantaInfrastructure(configuration);
		services.AddScoped<IParticipantService, ParticipantService>();
		services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));
		return services;
	}
}
