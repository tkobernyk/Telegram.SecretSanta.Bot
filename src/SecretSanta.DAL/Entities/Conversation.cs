using Newtonsoft.Json;

namespace SecretSanta.DAL.Entities;

public class Conversation
{
	[JsonProperty("id")]
	public string Id { get; set; } = Guid.NewGuid().ToString("D");

	[JsonProperty("eventId")]
	public string EventId { get; set; } = string.Empty;

	[JsonProperty("santaTelegramUserId")]
	public string SantaTelegramUserId { get; set; } = string.Empty;
	[JsonProperty("recipientTelegramUserId")]
	public string RecipientTelegramUserId { get; set; } = string.Empty;
	[JsonProperty("status")]
	public string Status { get; set; } = "Active";
	[JsonProperty("createdAtUtc")]
	public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
	[JsonProperty("updatedAtUtc")]
	public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
