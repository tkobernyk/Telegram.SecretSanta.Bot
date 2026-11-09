using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecretSanta.DAL;
using SecretSanta.Infrastructure.AI;
using SecretSanta.Infrastructure.Telegram;

namespace SecretSanta.Infrastructure
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddSecretSantaInfrastructure(this IServiceCollection services, IConfiguration configuration)
		{
			services.AddSingleton<TelegramClientFactory>();
			services.AddSingleton<ITelegramSender, TelegramSender>();

			// AI Gemini client
			var baseUrl = configuration["Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/";
			services.AddHttpClient("gemini", c => c.BaseAddress = new Uri(baseUrl));
			services.AddSingleton<IGeminiService, GeminiService>();

			services.AddSecretSantaDal(configuration);

			return services;
		}
	}
}
