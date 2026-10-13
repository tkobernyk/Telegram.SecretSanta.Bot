namespace SecretSanta.DAL.Cosmos;

public class CosmosConfiguration
{
	public const string SectionName = "Cosmos";
	public string Endpoint { get; set; } = string.Empty;
	public string Key { get; set; } = string.Empty;

	public string DatabaseName { get; set; } = string.Empty;
	public string ContainerName { get; set; } = string.Empty;
	public string ApplicationName { get; set; } = "SecretSanta";
}
