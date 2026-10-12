using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace SecretSanta.Infrastructure.Telegram
{
	public class TelegramSender : ITelegramSender
	{
		private readonly TelegramClientFactory _factory;

		public TelegramSender(TelegramClientFactory factory)
		{
			_factory = factory;
		}

		public async Task SendMessageAsync(string telegramUserId, string text)
		{
			if (long.TryParse(telegramUserId, out var chatId))
			{
		
				await _factory.Client.SendMessage(chatId, text);
			}
		}
	}
}
