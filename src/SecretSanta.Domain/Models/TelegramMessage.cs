namespace SecretSanta.Domain.Models;

public class TelegramMessage
{
	public long MessageId { get; set; }
	public TelegramUser? From { get; set; }
	public string? Text { get; set; }
}
