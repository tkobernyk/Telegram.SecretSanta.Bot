using Telegram.Bot;

namespace SecretSanta.Infrastructure.Telegram;

public class TelegramSender(TelegramClientFactory factory) : ITelegramSender
{
	readonly TelegramClientFactory _factory = factory;

	public async Task SendMessageAsync(string telegramUserId, string text)
	{
		if (long.TryParse(telegramUserId, out var chatId))
		{
			try
			{
				await _factory.Client.SendMessage(chatId, text);
			}
			catch (Exception)
			{
				throw new Exception($"Failed to send message to Telegram user {telegramUserId}");
			}
		}
	}
}
