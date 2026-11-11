using SecretSanta.Domain.Abstractions;
using SecretSanta.DAL.Entities;

namespace SecretSanta.Domain.Services;

public sealed class BudgetVoteOutcome
{
	public bool HasConsensus { get; set; }
	public string? OptionId { get; set; }
	public long? SelectedBudget { get; set; }
	public int TotalVotes { get; set; }
	public string? Message { get; set; }
}

public class VoteService : IVoteService
{
	public VoteSession PrepareVoteSession(VoteSession session)
	{
		if (session.Options == null || session.Options.Count == 0)
		{
			session.Options =
			[
				new VoteOption { Id = "250", Label = "250 грн", Value = 250 },
				new VoteOption { Id = "500", Label = "500 грн", Value = 500 },
				new VoteOption { Id = "750", Label = "750 грн", Value = 750 },
				new VoteOption { Id = "1000", Label = "1000 грн", Value = 1000 },
				new VoteOption { Id = "1500", Label = "1500 грн", Value = 1500 }
			];
		}

		return session;
	}

	public BudgetVoteOutcome EvaluateBudgetVotes(IEnumerable<Vote> votes, int expectedParticipantsCount)
	{
		var voteList = votes.ToList();
		var totalVotes = voteList.Count;
		if (totalVotes == 0)
		{
			return new BudgetVoteOutcome { HasConsensus = false, Message = "No votes yet." };
		}

		var groups = voteList
			.GroupBy(v => v.OptionId)
			.Select(group => new { OptionId = group.Key, Count = group.Count() })
			.OrderByDescending(group => group.Count)
			.ThenByDescending(group => long.TryParse(group.OptionId, out var value) ? value : 0)
			.ToList();

		var top = groups[0];
		if (expectedParticipantsCount > 0 && totalVotes < expectedParticipantsCount)
		{
			return new BudgetVoteOutcome
			{
				HasConsensus = false,
				TotalVotes = totalVotes,
				Message = $"Отримано {totalVotes} з {expectedParticipantsCount} голосів. Очікуємо на інших учасників."
			};
		}

		var hasMajority = top.Count * 2 > totalVotes;
		var hasConsensus = expectedParticipantsCount == 0
			? hasMajority
			: totalVotes == expectedParticipantsCount && top.Count == expectedParticipantsCount;
		var selectedOptionId = hasConsensus ? top.OptionId : null;
		var selectedBudget = selectedOptionId != null && long.TryParse(selectedOptionId, out var parsedBudget) ? parsedBudget : (long?)null;

		if (!hasConsensus)
		{
			var noConsensusMessage = expectedParticipantsCount == 0
				? "Немає мажоритарної відповіді. Потрібно більше голосів для консенсусу."
				: "Немає консолідованої відповіді. Учасники переходять до обговорення та організатор може перезапустити голосування.";
			return new BudgetVoteOutcome
			{
				HasConsensus = false,
				OptionId = null,
				SelectedBudget = null,
				TotalVotes = totalVotes,
				Message = noConsensusMessage
			};
		}

		return new BudgetVoteOutcome
		{
			HasConsensus = true,
			OptionId = selectedOptionId,
			SelectedBudget = selectedBudget,
			TotalVotes = totalVotes,
			Message = selectedBudget.HasValue ? $"Консолідовано: {selectedBudget} грн" : $"Консолідовано: {selectedOptionId}"
		};
	}
}
