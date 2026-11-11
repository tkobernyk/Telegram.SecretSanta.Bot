using System.Text.Json.Serialization;

namespace SecretSanta.Domain.Models;

public class TelegramMessage
{
	[JsonPropertyName("message_id")]
	public long MessageId { get; set; }
	[JsonPropertyName("from")]
	public TelegramUser? From { get; set; }
	[JsonPropertyName("text")]
	public string? Text { get; set; }
	public string? CallbackData { get; set; }
	public string? CallbackQueryId { get; set; }
}
