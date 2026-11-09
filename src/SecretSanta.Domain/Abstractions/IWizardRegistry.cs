namespace SecretSanta.Domain.Abstractions;

public interface IWizardRegistry
{
	IReadOnlyList<(string FieldName, string Prompt)> GetSteps(string wizardName);
}
