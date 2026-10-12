using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecretSanta.DAL;
using SecretSanta.Infrastructure.Telegram;

namespace SecretSanta.Infrastructure
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddSecretSantaInfrastructure(this IServiceCollection services, IConfiguration configuration)
		{
			services.AddSingleton<TelegramClientFactory>();
			services.AddSingleton<ITelegramSender, TelegramSender>();

			services.AddSecretSantaDal(configuration);
			return services;
		}
	}
}
