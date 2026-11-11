using System.ComponentModel.DataAnnotations;

namespace SecretSanta.Domain.Enums;

internal enum CommandType
{
	[Display(Name = "/start")]
	Start = 0,
	[Display(Name = "/start_vote")]
	StartVote = 1,
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
