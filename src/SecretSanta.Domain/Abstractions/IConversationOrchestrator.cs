namespace SecretSanta.Domain.Abstractions;

public interface IConversationOrchestrator
{
	Task StartConversationAsync(string eventId, string santaTelegramUserId, string recipientTelegramUserId, string initiatorTelegramUserId, CancellationToken cancellationToken = default);
	Task<string?> HandleIncomingMessageAsync(string eventId, string senderTelegramUserId, string text, CancellationToken cancellationToken = default);
	Task EndConversationAsync(string eventId, string initiatorTelegramUserId, CancellationToken cancellationToken = default);
}
