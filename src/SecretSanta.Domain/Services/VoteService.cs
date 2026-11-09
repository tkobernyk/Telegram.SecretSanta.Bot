using SecretSanta.Domain.Abstractions;
using SecretSanta.DAL.Abstractions;
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
	private readonly IVoteRepository _voteRepo;
	private readonly IParticipantRepository _participantRepo;
	private readonly IEventRepository _eventRepo;

	public VoteService(IVoteRepository voteRepo, IParticipantRepository participantRepo, IEventRepository eventRepo)
	{
		_voteRepo = voteRepo;
		_participantRepo = participantRepo;
		_eventRepo = eventRepo;
	}

	public async Task<VoteSession> CreateVoteSessionAsync(string eventId, VoteSession session, CancellationToken cancellationToken = default)
	{
		if (session.Options == null || session.Options.Count == 0)
		{
			session.Options = new List<VoteOption>
			{
				new() { Id = "250", Label = "250 грн", Value = 250 },
				new() { Id = "500", Label = "500 грн", Value = 500 },
				new() { Id = "750", Label = "750 грн", Value = 750 },
				new() { Id = "1000", Label = "1000 грн", Value = 1000 },
				new() { Id = "1500", Label = "1500 грн", Value = 1500 }
			};
		}

		await _voteRepo.CreateVoteSessionAsync(session, cancellationToken);
		var ev = await _eventRepo.GetByEventIdAsync(eventId, cancellationToken);
		if (ev != null)
		{
			ev.CurrentVoteSessionId = session.Id;
			ev.State = "Voting";
			ev.UpdatedAtUtc = DateTime.UtcNow;
			await _eventRepo.UpdateAsync(ev, cancellationToken);
		}
		return session;
	}

	public async Task SubmitVoteAsync(string eventId, string sessionId, Vote vote, CancellationToken cancellationToken = default)
	{
		await _voteRepo.CreateVoteAsync(vote, cancellationToken);
		var participant = await _participantRepo.GetByTelegramUserIdAsync(eventId, vote.VoterTelegramId.ToString(), cancellationToken);
		if (participant != null)
		{
			participant.PreferredGiftCategories = vote.OptionId;
			participant.RegistrationStatus = "Voted";
			participant.UpdatedAtUtc = DateTime.UtcNow;
			await _participantRepo.UpdateAsync(participant, cancellationToken);
		}
	}

	public async Task<string?> AggregateResultAsync(string eventId, string sessionId, CancellationToken cancellationToken = default)
	{
		var outcome = await EvaluateBudgetVotesAsync(eventId, sessionId, cancellationToken);
		return outcome.HasConsensus ? outcome.OptionId : null;
	}

	public async Task<BudgetVoteOutcome> EvaluateBudgetVotesAsync(string eventId, string sessionId, CancellationToken cancellationToken = default)
	{
		var votes = (await _voteRepo.GetVotesForSessionAsync(eventId, sessionId, cancellationToken)).ToList();
		var totalVotes = votes.Count;
		if (totalVotes == 0)
		{
			return new BudgetVoteOutcome { HasConsensus = false, Message = "No votes yet." };
		}

		var groups = votes
			.GroupBy(v => v.OptionId)
			.Select(g => new { OptionId = g.Key, Count = g.Count() })
			.OrderByDescending(g => g.Count)
			.ThenByDescending(g => long.TryParse(g.OptionId, out var value) ? value : 0)
			.ToList();

		var top = groups.First();
		var participants = (await _participantRepo.GetAllByEventAsync(eventId, cancellationToken)).ToList();
		if (participants.Count > 0 && totalVotes < participants.Count)
		{
			return new BudgetVoteOutcome { HasConsensus = false, TotalVotes = totalVotes, Message = $"Отримано {totalVotes} з {participants.Count} голосів. Очікуємо на інших учасників." };
		}

		var hasMajority = totalVotes > 0 && top.Count * 2 > totalVotes;
		var hasConsensus = participants.Count == 0
			? hasMajority
			: totalVotes == participants.Count && top.Count == participants.Count;
		var resultOptionId = hasConsensus ? top.OptionId : null;
		var selectedBudget = resultOptionId != null && long.TryParse(resultOptionId, out var parsedBudget) ? parsedBudget : (long?)null;

		if (participants.Count == 0 && !hasConsensus)
		{
			return new BudgetVoteOutcome
			{
				HasConsensus = false,
				OptionId = null,
				SelectedBudget = null,
				TotalVotes = totalVotes,
				Message = "Немає мажоритарної відповіді. Потрібно більше голосів для консенсусу."
			};
		}

		var ev = await _eventRepo.GetByEventIdAsync(eventId, cancellationToken);
		if (ev != null)
		{
			if (hasConsensus)
			{
				ev.State = "Draft";
				ev.BudgetConfirmed = selectedBudget;
				ev.UpdatedAtUtc = DateTime.UtcNow;
				await _eventRepo.UpdateAsync(ev, cancellationToken);
				return new BudgetVoteOutcome
				{
					HasConsensus = true,
					OptionId = resultOptionId,
					SelectedBudget = selectedBudget,
					TotalVotes = totalVotes,
					Message = selectedBudget.HasValue ? $"Консолідовано: {selectedBudget} грн" : $"Консолідовано: {resultOptionId}"
				};
			}

			ev.State = "BudgetDiscussion";
			ev.BudgetDiscussionCount += 1;
			ev.UpdatedAtUtc = DateTime.UtcNow;
			await _eventRepo.UpdateAsync(ev, cancellationToken);
		}

		return new BudgetVoteOutcome
		{
			HasConsensus = false,
			OptionId = null,
			SelectedBudget = null,
			TotalVotes = totalVotes,
			Message = "Немає консолідованої відповіді. Учасники переходять до обговорення та організатор може перезапустити голосування."
		};
	}
}
