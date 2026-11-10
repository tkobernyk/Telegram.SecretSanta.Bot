using Newtonsoft.Json;

namespace SecretSanta.DAL.Entities;

public class VoteSession
{
	[JsonProperty("id")]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	[JsonProperty("eventId")]
	public string EventId { get; set; } = string.Empty;

	[JsonProperty("options")]
	public List<VoteOption> Options { get; set; } = new();

	[JsonProperty("status")]
	public string Status { get; set; } = "Open"; // Open/Closed

	[JsonProperty("createdAtUtc")]
	public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

	[JsonProperty("closedAtUtc")]
	public DateTime? ClosedAtUtc { get; set; }

	[JsonProperty("resultOptionId")]
	public string? ResultOptionId { get; set; }
}
