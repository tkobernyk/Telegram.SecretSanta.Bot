using Newtonsoft.Json;

namespace SecretSanta.DAL.Entities;

public class ConversationMessage
{
	[JsonProperty("id")]
	public string Id { get; set; } = Guid.NewGuid().ToString("D");

	[JsonProperty("eventId")]
	public string EventId { get; set; } = string.Empty;

	public string ConversationId { get; set; } = string.Empty;
	public string SenderTelegramUserId { get; set; } = string.Empty;
	public string SenderDisplayName { get; set; } = string.Empty;
	public string Text { get; set; } = string.Empty;
	public string MessageType { get; set; } = "UserMessage";
	public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
