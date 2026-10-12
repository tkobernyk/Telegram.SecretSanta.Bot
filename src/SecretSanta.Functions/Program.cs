using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Configuration;

namespace SecretSanta.Functions
{
	public class AppSettings
	{
		public string? Telegram__BotToken { get; set; }
		public string? Telegram__WebhookSecret { get; set; }
		public string? Telegram__OrganizerTelegramId { get; set; }
		public string? Cosmos__Endpoint { get; set; }
		public string? Cosmos__DatabaseName { get; set; }
		public string? Gemini__ApiKey { get; set; }
		public string? Gemini__Model { get; set; }
	}

	internal static class Program
	{
		public static async System.Threading.Tasks.Task Main(string[] args)
		{
			var host = new HostBuilder()
				.ConfigureFunctionsWorkerDefaults()
				.ConfigureAppConfiguration((context, builder) =>
				{
					builder.AddEnvironmentVariables();
				})
				.ConfigureServices((context, services) =>
				{
					services.AddHttpClient();
					services.AddSingleton<IConfiguration>(context.Configuration);
					services.Configure<AppSettings>(context.Configuration);

					// Basic startup validation for required settings
					var cfg = context.Configuration;
					var missing = new System.Collections.Generic.List<string>();
					if (string.IsNullOrWhiteSpace(cfg["Telegram__BotToken"])) missing.Add("Telegram__BotToken");
					if (string.IsNullOrWhiteSpace(cfg["Telegram__WebhookSecret"])) missing.Add("Telegram__WebhookSecret");
					if (string.IsNullOrWhiteSpace(cfg["Telegram__OrganizerTelegramId"])) missing.Add("Telegram__OrganizerTelegramId");

					if (missing.Count > 0)
					{
						// We can't resolve ILoggerFactory here reliably, write to Console and throw to fail-fast.
						Console.Error.WriteLine($"Missing required configuration: {string.Join(", ", missing)}");
						throw new ApplicationException("Missing required configuration. See environment variables or local.settings.json.example.");
					}
				})
				.ConfigureLogging(logging => logging.AddConsole())
				.Build();

			await host.RunAsync();
		}
	}
}
