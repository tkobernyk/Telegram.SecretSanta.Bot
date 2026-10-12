using System.Threading.Tasks;

namespace SecretSanta.Infrastructure.Telegram
{
	public interface ITelegramSender
	{
		Task SendMessageAsync(string telegramUserId, string text);
	}
}
