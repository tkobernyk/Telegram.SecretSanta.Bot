namespace SecretSanta.Domain.Handlers.TelegramEndpoint;

internal enum TelegramCallbackType
{
	Unknown = 0,
	VoteStart,
	BudgetVote,
	GiftReady,
	GiftProgress,
	ChatRecipient,
	ChatSanta,
	ProfileView,
	WishlistEdit,
	ConversationEnd,
	StartCreate,
	StartJoin,
	Calendar
}