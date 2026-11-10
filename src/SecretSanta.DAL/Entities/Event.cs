using Newtonsoft.Json;

namespace SecretSanta.DAL.Entities;

public class Event
{
	[JsonProperty("id")]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	[JsonProperty("eventId")]
	public string EventId { get; set; } = string.Empty;

	[JsonProperty("name")]
	public string Name { get; set; } = string.Empty;

	[JsonProperty("joinCode")]
	public string JoinCode { get; set; } = string.Empty;

	[JsonProperty("organizerTelegramId")]
	public long OrganizerTelegramId { get; set; }

	[JsonProperty("createdByTelegramId")]
	public long CreatedByTelegramId { get; set; }

	[JsonProperty("state")]
	public string State { get; set; } = "Registration";

	[JsonProperty("currentVoteSessionId")]
	public string? CurrentVoteSessionId { get; set; }

	[JsonProperty("budgetConfirmed")]
	public long? BudgetConfirmed { get; set; }

	[JsonProperty("deliveryTimeUtc")]
	public DateTime? DeliveryTimeUtc { get; set; }

	[JsonProperty("deliveryLocation")]
	public string? DeliveryLocation { get; set; }

	[JsonProperty("deliveryRequestedAtUtc")]
	public DateTime? DeliveryRequestedAtUtc { get; set; }

	[JsonProperty("deliveryMessageSentUtc")]
	public DateTime? DeliveryMessageSentUtc { get; set; }

	[JsonProperty("giftStatusSummary")]
	public string? GiftStatusSummary { get; set; }

	[JsonProperty("budgetDiscussionCount")]
	public int BudgetDiscussionCount { get; set; }

	[JsonProperty("createdAtUtc")]
	public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

	[JsonProperty("updatedAtUtc")]
	public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

	[JsonProperty("history")]
	public List<EventStateChange> History { get; set; } = new();
}

public class EventStateChange
{
	[JsonProperty("state")] public string State { get; set; } = string.Empty;
	[JsonProperty("changedBy")] public long? ChangedBy { get; set; }
	[JsonProperty("changedAtUtc")] public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
	[JsonProperty("note")] public string? Note { get; set; }
}
