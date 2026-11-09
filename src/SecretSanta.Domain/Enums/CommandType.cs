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
	[Display(Name = "/cancel")]
	Cancel = 3,
	[Display(Name = "/ask_santa")]
	AskSanta = 4,
	[Display(Name = "/ask_recipient")]
	AskRecipient = 5,
	[Display(Name = "/start_vote")]
	StartVote = 6,
}
