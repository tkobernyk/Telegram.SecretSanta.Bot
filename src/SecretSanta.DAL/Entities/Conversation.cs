using Newtonsoft.Json;

namespace SecretSanta.DAL.Entities;

public class Conversation
{
	[JsonProperty("id")]
	public string Id { get; set; } = Guid.NewGuid().ToString("D");

	[JsonProperty("eventId")]
	public string EventId { get; set; } = string.Empty;

	public string SantaTelegramUserId { get; set; } = string.Empty;
	public string RecipientTelegramUserId { get; set; } = string.Empty;
	public string Status { get; set; } = "Active"; // Active, Closed
	public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
