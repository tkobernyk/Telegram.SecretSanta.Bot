using MediatR;
using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Enums;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.Domain.Handlers.Commands.Draft;
using SecretSanta.Domain.Handlers.Commands.Workflow;
using SecretSanta.Infrastructure.Telegram;
using Telegram.Bot.Types.ReplyMarkups;
using SecretSanta.DAL.Abstractions.Repositories;

namespace SecretSanta.Domain.Handlers.Commands.Voting;

public sealed class SubmitBudgetVoteHandler : IRequestHandler<SubmitBudgetVoteRequest, CommandResponse>
{
	private static readonly long[] AllowedBudgets = [250, 500, 750, 1000, 1500];
	private readonly IEventRepository _eventRepository;
	private readonly IParticipantRepository _participantRepository;
	private readonly IVoteRepository _voteRepository;
	private readonly IVoteService _voteService;
	private readonly IAssignmentRepository _assignmentRepository;
	private readonly IDraftService _draftService;
	private readonly IMediator _mediator;
	private readonly ITelegramSender _telegramSender;

	public SubmitBudgetVoteHandler(IEventRepository eventRepository, IParticipantRepository participantRepository, IVoteRepository voteRepository, IVoteService voteService, IAssignmentRepository assignmentRepository, IDraftService draftService, IMediator mediator, ITelegramSender telegramSender)
	{
		_eventRepository = eventRepository;
		_participantRepository = participantRepository;
		_voteRepository = voteRepository;
		_voteService = voteService;
		_assignmentRepository = assignmentRepository;
		_draftService = draftService;
		_mediator = mediator;
		_telegramSender = telegramSender;
	}

	public async Task<CommandResponse> Handle(SubmitBudgetVoteRequest request, CancellationToken cancellationToken)
	{
		if (!AllowedBudgets.Contains(request.Budget))
			return new CommandResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Оберіть одну із запропонованих сум." };

		var ev = await _eventRepository.GetByEventIdAsync(request.EventId, cancellationToken);
		if (ev == null) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Подію не знайдено." };
		if (!string.Equals(ev.State, EventWorkflowState.Voting, StringComparison.OrdinalIgnoreCase))
			return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NoContent };
		if (string.IsNullOrWhiteSpace(ev.CurrentVoteSessionId))
			return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NoContent };

		var participant = await _participantRepository.GetByTelegramUserIdAsync(request.EventId, request.TelegramUserId.ToString(), cancellationToken);
		if (participant == null) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.Forbidden, ResponseMessage = "До голосування можуть долучатися лише учасники події." };

		var vote = new Vote
		{
			Id = $"{request.EventId}:{ev.CurrentVoteSessionId}:{request.TelegramUserId}",
			EventId = request.EventId,
			VoteSessionId = ev.CurrentVoteSessionId,
			VoterTelegramId = request.TelegramUserId,
			OptionId = request.Budget.ToString()
		};
		await _voteRepository.CreateVoteAsync(vote, cancellationToken);
		participant.BudgetStatus = vote.OptionId;
		participant.RegistrationStatus = "Voted";
		participant.UpdatedAtUtc = DateTime.UtcNow;
		await _participantRepository.UpdateAsync(participant, cancellationToken);

		var sessionVotes = await _voteRepository.GetVotesForSessionAsync(request.EventId, ev.CurrentVoteSessionId, cancellationToken);
		var participantsCount = (await _participantRepository.GetAllByEventAsync(request.EventId, cancellationToken)).Count();
		var votes = _voteService.EvaluateBudgetVotes(sessionVotes, participantsCount);
		if (!votes.HasConsensus)
		{
			if (participantsCount > 0 && votes.TotalVotes >= participantsCount)
			{
				var noConsensusTransition = await _mediator.Send(new ApplyEventTransitionRequest
				{
					EventId = request.EventId,
					Trigger = EventWorkflowTrigger.BudgetConsensusNotReached,
					ChangedByTelegramId = request.TelegramUserId,
					Note = "Budget consensus not reached"
				}, cancellationToken);
				if (noConsensusTransition.StatusCode == System.Net.HttpStatusCode.OK)
				{
					var refreshedEvent = await _eventRepository.GetByEventIdAsync(request.EventId, cancellationToken);
					if (refreshedEvent != null)
					{
						refreshedEvent.BudgetDiscussionCount += 1;
						await _eventRepository.UpdateAsync(refreshedEvent, cancellationToken);
					}

					var participantsForDiscussion = (await _participantRepository.GetAllByEventAsync(request.EventId, cancellationToken)).ToList();
					var voteStatisticsMessage = BuildNoConsensusStatisticsMessage(sessionVotes);
					foreach (var participantForDiscussion in participantsForDiscussion)
					{
						await _telegramSender.SendMessageAsync(participantForDiscussion.TelegramUserId, voteStatisticsMessage);
					}
				}
			}

			return new CommandResponse { ResponseMessage = votes.Message ?? $"Ваш вибір {request.Budget} грн збережено." };
		}

		var consensusTransition = await _mediator.Send(new ApplyEventTransitionRequest
		{
			EventId = request.EventId,
			Trigger = EventWorkflowTrigger.BudgetConsensusReached,
			ChangedByTelegramId = request.TelegramUserId,
			Note = "Budget consensus reached"
		}, cancellationToken);
		if (consensusTransition.StatusCode != System.Net.HttpStatusCode.OK)
		{
			return consensusTransition;
		}

		var draftParticipants = (await _participantRepository.GetAllByEventAsync(request.EventId, cancellationToken)).ToList();
		var generatedAssignments = _draftService.BuildAssignments(request.EventId, draftParticipants.Select(item => item.TelegramUserId).ToList());
		await _assignmentRepository.CreateAssignmentsAsync(generatedAssignments, cancellationToken);
		var draftTransition = await _mediator.Send(new ApplyEventTransitionRequest
		{
			EventId = request.EventId,
			Trigger = EventWorkflowTrigger.DraftStarted,
			ChangedByTelegramId = ev.OrganizerTelegramId,
			Note = "Draft started"
		}, cancellationToken);
		if (draftTransition.StatusCode != System.Net.HttpStatusCode.OK)
		{
			return draftTransition;
		}

		var assignments = await _assignmentRepository.GetAssignmentsByEventAsync(request.EventId, cancellationToken);
		var participants = (await _participantRepository.GetAllByEventAsync(request.EventId, cancellationToken)).ToDictionary(p => p.TelegramUserId);
		var deadlineText = ev.GiftPreparationDeadlineUtc.HasValue
			? $"Підготуйте подарунок до {ev.GiftPreparationDeadlineUtc.Value:dd.MM.yyyy}."
			: null;
		foreach (var assignment in assignments.GroupBy(a => a.SantaTelegramUserId).Select(group => group.First()))
		{
			if (!participants.TryGetValue(assignment.SantaTelegramUserId.ToString(), out var santa) || !participants.TryGetValue(assignment.RecipientTelegramUserId.ToString(), out var recipient))
				continue;

			var keyboard = new InlineKeyboardMarkup(
			[
				[InlineKeyboardButton.WithCallbackData("💬Уточнити в Отримувача", "chat:recipient")],
				[InlineKeyboardButton.WithCallbackData("🎅Написати Миколайкові", "chat:santa")],
				[InlineKeyboardButton.WithCallbackData("🎁Подарунок вже готовий", "gift:ready")]
			]);

			var message = $"""
				Бюджет узгоджено: {votes.SelectedBudget} грн
				Ваш отримувач: {recipient.DisplayName}
				Список побажань: {recipient.WishlistText ?? "Не заповнений"}
				Хобі: {recipient.Hobbies ?? "Не заповнені"}
				Улюблені категорії подарунків: {recipient.PreferredGiftCategories ?? "Не заповнені"}
				Улюблені кольори: {recipient.PreferredColors ?? "Не заповнені"}
				Уникати категорії: {recipient.AvoidCategories ?? "Не заповнені"}
				Інші побажання: {recipient.OtherPreferences ?? "Не заповнені"}
				{deadlineText}

				Використайте кнопки нижче для анонімного спілкування та оновлення статусу подарунка.
				""";
				
			await _telegramSender.SendMessageAsync(santa.TelegramUserId, message, keyboard);
		}

		var current = await _eventRepository.GetByEventIdAsync(request.EventId, cancellationToken);
		if (current != null)
		{
			current.BudgetConfirmed = votes.SelectedBudget;
			await _eventRepository.UpdateAsync(current, cancellationToken);
		}

		return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NoContent };
	}

	private static string BuildNoConsensusStatisticsMessage(IEnumerable<Vote> votes)
	{
		var statistics = votes
			.GroupBy(vote => vote.OptionId)
			.Select(group => new
			{
				Budget = long.TryParse(group.Key, out var parsedBudget) ? parsedBudget : 0,
				VotesCount = group.Count()
			})
			.OrderByDescending(item => item.VotesCount)
			.ThenByDescending(item => item.Budget)
			.ToList();

		if (statistics.Count == 0)
		{
			return "Немає консолідованої відповіді. Поки що немає жодного голосу для обговорення.";
		}

		var rows = statistics
			.Select(item => $"• {item.Budget} грн — {item.VotesCount} голос(ів)");

		return $"Немає консолідованої відповіді. Результати голосування:\n{string.Join("\n", rows)}\n\nОбговоріть бюджет у групі та перезапустіть голосування.";
	}
}
