using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Services;
using SecretSanta.Infrastructure;

namespace SecretSanta.Domain;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddSecretSantaDomain(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSecretSantaInfrastructure(configuration);
		services.AddScoped<IParticipantService, ParticipantService>();
		services.AddSingleton<IPendingUserInputService, PendingUserInputService>();
		services.AddSingleton<IConversationOrchestrator, ConversationOrchestrator>();
		services.AddSingleton<IWizardRegistry, WizardRegistry>();

		services.AddSingleton<IVoteService, VoteService>();
		services.AddSingleton<IDraftService, DraftService>();
		services.AddSingleton<IEventService, EventService>();
		services.AddSingleton<IAssignmentService, InMemoryAssignmentService>();

		services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));
		return services;
	}
}
