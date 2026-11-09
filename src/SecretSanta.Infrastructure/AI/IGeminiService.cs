using System.Threading;
using System.Threading.Tasks;

namespace SecretSanta.Infrastructure.AI;

public interface IGeminiService
{
	/// <summary>
	/// Sends the prompt to Gemini (or configured LLM) and returns the generated text.
	/// </summary>
	Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);
}
