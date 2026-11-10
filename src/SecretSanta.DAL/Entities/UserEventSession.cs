using Newtonsoft.Json;

namespace SecretSanta.DAL.Entities;

public sealed class UserEventSession
{
	[JsonProperty("id")]
	public string Id { get; set; } = string.Empty;

	[JsonProperty("telegramUserId")]
	public string TelegramUserId { get; set; } = string.Empty;

	[JsonProperty("eventId")]
	public string EventId { get; set; } = string.Empty;

	[JsonProperty("updatedAtUtc")]
	public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
