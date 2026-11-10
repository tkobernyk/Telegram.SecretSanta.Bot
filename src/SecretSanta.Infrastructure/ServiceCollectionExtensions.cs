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

			// Groq AI client
			var baseUrl = configuration["Groq:BaseUrl"] ?? configuration["Values:Groq__BaseUrl"] ?? "https://api.groq.com/openai/v1/";
			if (!baseUrl.EndsWith('/'))
			{
				baseUrl += "/";
			}
			services.AddHttpClient("groq", c => c.BaseAddress = new Uri(baseUrl));
			services.AddSingleton<ITextGenerationService, GroqService>();

			services.AddSecretSantaDal(configuration);

			return services;
		}
	}
}
