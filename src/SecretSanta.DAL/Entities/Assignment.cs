using Newtonsoft.Json;

namespace SecretSanta.DAL.Entities
{
	public class Assignment
	{
		[JsonProperty("id")]
		public string Id { get; set; } = Guid.NewGuid().ToString();

		[JsonProperty("eventId")]
		public string EventId { get; set; } = string.Empty;

		[JsonProperty("draftRunId")]
		public string DraftRunId { get; set; } = string.Empty;

		[JsonProperty("santaTelegramUserId")]
		public long SantaTelegramUserId { get; set; }

		[JsonProperty("recipientTelegramUserId")]
		public long RecipientTelegramUserId { get; set; }

		[JsonProperty("createdAtUtc")]
		public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
	}
}
