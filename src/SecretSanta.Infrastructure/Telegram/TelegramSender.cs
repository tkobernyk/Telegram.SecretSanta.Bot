using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace SecretSanta.Infrastructure.Telegram;

public class TelegramSender(TelegramClientFactory factory) : ITelegramSender
{
	readonly TelegramClientFactory _factory = factory;

	public async Task SendMessageAsync(string telegramUserId, string text, ReplyMarkup? replyMarkup = null)
	{
		if (long.TryParse(telegramUserId, out var chatId))
		{
			try
			{
				await _factory.Client.SendMessage(chatId, text, replyMarkup: replyMarkup);
			}
			catch (Exception)
			{
				throw new Exception($"Failed to send message to Telegram user {telegramUserId}");
			}
		}
	}

	public async Task AnswerCallbackQueryAsync(string callbackQueryId, string? text = null)
	{
		if (!string.IsNullOrWhiteSpace(callbackQueryId))
		{
			await _factory.Client.AnswerCallbackQuery(callbackQueryId, text: text);
	}
	}
}
