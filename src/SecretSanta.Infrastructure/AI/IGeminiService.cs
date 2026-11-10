using System.Threading;
using System.Threading.Tasks;

namespace SecretSanta.Infrastructure.AI;

public interface ITextGenerationService
{
	/// <summary>
	/// Sends the prompt to the configured text-generation provider and returns the generated text.
	/// </summary>
	Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default, Func<CancellationToken, Task>? onSecondAttempt = null);
}
