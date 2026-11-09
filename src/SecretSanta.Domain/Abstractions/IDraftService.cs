using System.Threading;
using System.Threading.Tasks;

namespace SecretSanta.Domain.Abstractions;

public interface IDraftService
{
	Task StartDraftAsync(string eventId, long initiatorTelegramId, CancellationToken cancellationToken = default);
}
