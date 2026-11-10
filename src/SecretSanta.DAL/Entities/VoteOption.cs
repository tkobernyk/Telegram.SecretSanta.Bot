using Newtonsoft.Json;

namespace SecretSanta.DAL.Entities;

public class VoteOption
{
	[JsonProperty("id")]
	public string Id { get; set; } = Guid.NewGuid().ToString();
	[JsonProperty("label")]
	public string Label { get; set; } = string.Empty;
	[JsonProperty("value")]
	public long Value { get; set; }
}
