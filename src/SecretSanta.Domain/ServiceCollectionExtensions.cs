using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Services;
using SecretSanta.Domain.Services.Workflow;
using SecretSanta.Domain.Services.Workflow.Rules;
using SecretSanta.Infrastructure;

namespace SecretSanta.Domain;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddSecretSantaDomain(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSecretSantaInfrastructure(configuration);
		services.AddSingleton<IConversationOrchestrator, ConversationOrchestrator>();
		services.AddSingleton<IWizardRegistry, WizardRegistry>();

		services.AddSingleton<IVoteService, VoteService>();
		services.AddSingleton<IDraftService, DraftService>();

		services.AddSingleton<IEventStateTransitionRuleFactory, EventStateTransitionRuleFactory>();
		services.AddSingleton<IEventStateMachine, EventStateMachine>();
		services.AddSingleton<IEventStateTransitionRule, RegistrationVoteSessionStartedRule>();
		services.AddSingleton<IEventStateTransitionRule, BudgetConfirmationVoteSessionStartedRule>();
		services.AddSingleton<IEventStateTransitionRule, BudgetDiscussionVoteSessionStartedRule>();
		services.AddSingleton<IEventStateTransitionRule, VotingBudgetConsensusReachedRule>();
		services.AddSingleton<IEventStateTransitionRule, VotingBudgetConsensusNotReachedRule>();
		services.AddSingleton<IEventStateTransitionRule, VotingDraftStartedRule>();
		services.AddSingleton<IEventStateTransitionRule, BudgetDiscussionDraftStartedRule>();
		services.AddSingleton<IEventStateTransitionRule, DraftDraftStartedRule>();
		services.AddSingleton<IEventStateTransitionRule, DraftGiftProgressUpdatedRule>();
		services.AddSingleton<IEventStateTransitionRule, PreparingGiftProgressUpdatedRule>();
		services.AddSingleton<IEventStateTransitionRule, DraftAllGiftsReadyRule>();
		services.AddSingleton<IEventStateTransitionRule, PreparingAllGiftsReadyRule>();
		services.AddSingleton<IEventStateTransitionRule, AwaitingDeliveryDeliveryInfoProvidedRule>();

		services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));
		return services;
	}
}
