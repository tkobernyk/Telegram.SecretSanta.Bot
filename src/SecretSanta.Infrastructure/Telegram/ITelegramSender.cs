using Telegram.Bot.Types.ReplyMarkups;

namespace SecretSanta.Infrastructure.Telegram
{
	public interface ITelegramSender
	{
		Task SendMessageAsync(string telegramUserId, string text, ReplyMarkup? replyMarkup = null);
	}
}
