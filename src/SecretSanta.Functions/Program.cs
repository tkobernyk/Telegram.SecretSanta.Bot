using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecretSanta.Domain;
using SecretSanta.Infrastructure;
using SecretSanta.DAL;
using SecretSanta.Functions.Services;

namespace SecretSanta.Functions;

public class AppSettings
{
	public string? Telegram__BotToken { get; set; }
	public string? Telegram__WebhookSecret { get; set; }
	public string? Telegram__OrganizerTelegramId { get; set; }
	public string? Cosmos__Endpoint { get; set; }
	public string? Cosmos__DatabaseName { get; set; }
	public string? Groq__ApiKey { get; set; }
	public string? Groq__Model { get; set; }
}

internal static class Program
{
	public static async Task Main(string[] args)
	{
		var host = new HostBuilder()
			.ConfigureFunctionsWorkerDefaults()
			.ConfigureAppConfiguration((context, builder) =>
			{
				builder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
					.AddEnvironmentVariables();
			})
			.ConfigureServices((context, services) =>
			{
				services.AddHttpClient();
				services.Configure<AppSettings>(context.Configuration);

				services.AddSecretSantaDomain(context.Configuration);
				
				services.AddTransient<ITelegramWebhookService, TelegramWebhookService>();

				var cfg = context.Configuration;
				string GetCfg(string key) => cfg[key] ?? cfg[$"{key}"] ?? string.Empty;
				var missing = new List<string>();
				if (string.IsNullOrWhiteSpace(GetCfg("Telegram:BotToken"))) missing.Add("Telegram__BotToken");
				if (string.IsNullOrWhiteSpace(GetCfg("Telegram:WebhookSecret"))) missing.Add("Telegram__WebhookSecret");
				if (string.IsNullOrWhiteSpace(GetCfg("Telegram:OrganizerTelegramId"))) missing.Add("Telegram__OrganizerTelegramId");
				if (string.IsNullOrWhiteSpace(GetCfg("Cosmos:Endpoint"))) missing.Add("Cosmos__Endpoint");
				if (string.IsNullOrWhiteSpace(GetCfg("Cosmos:Key"))) missing.Add("Cosmos__Key");
				if (string.IsNullOrWhiteSpace(GetCfg("Cosmos:DatabaseName"))) missing.Add("Cosmos__DatabaseName");

				if (missing.Count > 0)
				{
					Console.Error.WriteLine($"Missing required configuration: {string.Join(", ", missing)}");
					throw new ApplicationException("Missing required configuration. See environment variables or local.settings.json.example.");
				}
			})
			.ConfigureLogging(logging => logging.AddConsole())
			.Build();

		await host.RunAsync();
	}
}
