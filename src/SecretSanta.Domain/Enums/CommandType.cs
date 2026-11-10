using System.ComponentModel.DataAnnotations;

namespace SecretSanta.Domain.Enums;

internal enum CommandType
{
	[Display(Name = "/start")]
	Start = 0,
	[Display(Name = "/profile")]
	Profile = 1,
	[Display(Name = "/wishlist")]
	Wishlist = 2,
	[Display(Name = "/ask_santa")]
	AskSanta = 3,
	[Display(Name = "/ask_recipient")]
	AskRecipient = 4,
	[Display(Name = "/start_vote")]
	StartVote = 5,
	[Display(Name = "/gift_ready")]
	GiftReady = 6,
	[Display(Name = "/gift_progress")]
	GiftProgress = 7,
	[Display(Name = "/vote_250")]
	Vote250 = 8,
	[Display(Name = "/vote_500")]
	Vote500 = 9,
	[Display(Name = "/vote_750")]
	Vote750 = 10,
	[Display(Name = "/vote_1000")]
	Vote1000 = 11,
	[Display(Name = "/vote_1500")]
	Vote1500 = 12,
}

public static class EventWorkflowState
{
	public const string Registration = "Registration";
	public const string Voting = "Voting";
	public const string BudgetConfirmation = "BudgetConfirmation";
	public const string BudgetDiscussion = "BudgetDiscussion";
	public const string Draft = "Draft";
	public const string Preparing = "Preparing";
	public const string AwaitingDelivery = "AwaitingDelivery";
	public const string Done = "Done";
	public const string Completed = "Completed";
	public const string Cancelled = "Cancelled";
}

public static class GiftStatusValue
{
	public const string NotStarted = "NotStarted";
	public const string Ready = "Ready";
	public const string InProgress = "InProgress";
}
