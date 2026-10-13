using Telegram.Bot;
using Microsoft.Extensions.Configuration;

namespace SecretSanta.Infrastructure.Telegram;

public class TelegramClientFactory
{
	readonly TelegramBotClient _client;

	public TelegramClientFactory(IConfiguration configuration)
	{
		var token = configuration["Values:Telegram__BotToken"];
		if (string.IsNullOrEmpty(token)) throw new ArgumentException("Telegram__BotToken is required in configuration.");
		_client = new TelegramBotClient(token);
	}

	public TelegramBotClient Client => _client;
}
