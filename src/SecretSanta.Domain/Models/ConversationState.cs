namespace SecretSanta.Domain.Models;

public class ConversationState
{
	public string WizardName { get; set; } = string.Empty;
	public int StepIndex { get; set; }
	public Dictionary<string, string> Data { get; set; } = new();
}
