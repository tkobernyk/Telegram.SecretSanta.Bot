using Newtonsoft.Json;

namespace SecretSanta.DAL.Entities;

public class Vote
{
	[JsonProperty("id")]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	[JsonProperty("eventId")]
	public string EventId { get; set; } = string.Empty;

	[JsonProperty("voteSessionId")]
	public string VoteSessionId { get; set; } = string.Empty;

	[JsonProperty("voterTelegramId")]
	public long VoterTelegramId { get; set; }

	[JsonProperty("optionId")]
	public string OptionId { get; set; } = string.Empty;

	[JsonProperty("createdAtUtc")]
	public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
