using System.Text.Json.Serialization;

namespace SecretSanta.Domain.Models;

public class TelegramUser
{
	public long Id { get; set; }
	[JsonPropertyName("first_name")]
	public string? FirstName { get; set; }
	[JsonPropertyName("last_name")]
	public string? LastName { get; set; }
	[JsonPropertyName("username")]
	public string? Username { get; set; }
}
