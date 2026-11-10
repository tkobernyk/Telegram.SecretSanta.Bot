using Newtonsoft.Json;
using SecretSanta.DAL.Cosmos;

namespace SecretSanta.DAL.Entities
{
	[CosmosContainer("VoteSessions")]
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

	public class VoteOption
	{
		[JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
		[JsonProperty("label")] public string Label { get; set; } = string.Empty;
		[JsonProperty("value")] public long Value { get; set; }
	}
}
