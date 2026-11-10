using Newtonsoft.Json;
using SecretSanta.DAL.Cosmos;

namespace SecretSanta.DAL.Entities;

public class Participant
{
	[JsonProperty("id")]
	public string Id { get; set; } = Guid.NewGuid().ToString();
	[JsonProperty("eventId")]
	public string EventId { get; set; }
	public string TelegramUserId { get; set; }
	public string DisplayName { get; set; }
	public string? WishlistText { get; set; }
	public string? Hobbies { get; set; }
	public string? PreferredGiftCategories { get; set; }
	public string? AvoidCategories { get; set; }
	public string? PreferredColors { get; set; }
	public string? OtherPreferences { get; set; }
	public string RegistrationStatus { get; set; }
	public string GiftStatus { get; set; } = "NotStarted";
	public DateTime? GiftReadyAtUtc { get; set; }
	public DateTime CreatedAtUtc { get; set; }
	public DateTime UpdatedAtUtc { get; set; }

	public Participant()
	{
		Id = Guid.NewGuid().ToString("D");
		EventId = string.Empty;
		TelegramUserId = string.Empty;
		DisplayName = string.Empty;
		RegistrationStatus = "Registered";
		GiftStatus = "NotStarted";
		CreatedAtUtc = DateTime.UtcNow;
		UpdatedAtUtc = DateTime.UtcNow;
	}
}
