using SecretSanta.Domain.Abstractions;

namespace SecretSanta.Domain.Services;

public class WizardRegistry : IWizardRegistry
{
	public IReadOnlyList<(string FieldName, string Prompt)> GetSteps(string wizardName)
	{
		if (string.Equals(wizardName, "wishlist", StringComparison.OrdinalIgnoreCase))
		{
			return new List<(string, string)>
			{
				("WishlistText", "Напишіть ваші побажання щодо подарунку (коротко):"),
				("Hobbies", "Напишіть ваші хобі (через кому або коротко):"),
				("PreferredGiftCategories", "Які категорії подарунків вам подобаються?") ,
				("AvoidCategories", "Які категорії слід уникати?"),
				("PreferredColors", "Улюблені кольори:"),
				("OtherPreferences", "Інші побажання або обмеження:")
			};
		}

		return [];
	}
}
