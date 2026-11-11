namespace SecretSanta.Domain.Handlers.TelegramEndpoint;

internal sealed class TelegramCallbackParseResult
{
	public TelegramCallbackType CallbackType { get; init; }
	public long? BudgetValue { get; init; }
}