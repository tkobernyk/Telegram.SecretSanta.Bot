namespace SecretSanta.Domain.Enums;

public enum EventWorkflowTrigger
{
	VoteSessionStarted,
	BudgetConsensusReached,
	BudgetConsensusNotReached,
	DraftStarted,
	BudgetConfirmedByAll,
	GiftProgressUpdated,
	AllGiftsReady,
	DeliveryInfoProvided
}
