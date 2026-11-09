using MediatR;
using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.Domain.Handlers.Commands.Draft;
using SecretSanta.Infrastructure.Telegram;
using Telegram.Bot.Types.ReplyMarkups;

namespace SecretSanta.Domain.Handlers.Commands.Voting;

public sealed class SubmitBudgetVoteHandler : IRequestHandler<SubmitBudgetVoteRequest, CommandResponse>
{
	private static readonly long[] AllowedBudgets = [250, 500, 750, 1000, 1500];
	private readonly IEventRepository _eventRepository;
	private readonly IParticipantRepository _participantRepository;
	private readonly IVoteService _voteService;
	private readonly IAssignmentRepository _assignmentRepository;
	private readonly IDraftService _draftService;
	private readonly ITelegramSender _telegramSender;

	public SubmitBudgetVoteHandler(IEventRepository eventRepository, IParticipantRepository participantRepository, IVoteService voteService, IAssignmentRepository assignmentRepository, IDraftService draftService, ITelegramSender telegramSender)
	{
		_eventRepository = eventRepository;
		_participantRepository = participantRepository;
		_voteService = voteService;
		_assignmentRepository = assignmentRepository;
		_draftService = draftService;
		_telegramSender = telegramSender;
	}

	public async Task<CommandResponse> Handle(SubmitBudgetVoteRequest request, CancellationToken cancellationToken)
	{
		if (!AllowedBudgets.Contains(request.Budget))
			return new CommandResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Оберіть одну із запропонованих сум." };

		var ev = await _eventRepository.GetByEventIdAsync(request.EventId, cancellationToken);
		if (ev == null) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Подію не знайдено." };
		if (string.IsNullOrWhiteSpace(ev.CurrentVoteSessionId))
			return new CommandResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Зараз голосування не відкрите." };

		var participant = await _participantRepository.GetByTelegramUserIdAsync(request.EventId, request.TelegramUserId.ToString(), cancellationToken);
		if (participant == null) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.Forbidden, ResponseMessage = "До голосування можуть долучатися лише учасники події." };

		await _voteService.SubmitVoteAsync(request.EventId, ev.CurrentVoteSessionId, new Vote
		{
			Id = $"{request.EventId}:{ev.CurrentVoteSessionId}:{request.TelegramUserId}",
			EventId = request.EventId,
			VoteSessionId = ev.CurrentVoteSessionId,
			VoterTelegramId = request.TelegramUserId,
			OptionId = request.Budget.ToString()
		}, cancellationToken);

		var votes = await _voteService.EvaluateBudgetVotesAsync(request.EventId, ev.CurrentVoteSessionId, cancellationToken);
		if (!votes.HasConsensus)
			return new CommandResponse { ResponseMessage = votes.Message ?? $"Ваш вибір {request.Budget} грн збережено." };

		await _draftService.StartDraftAsync(request.EventId, ev.OrganizerTelegramId, cancellationToken);
		var assignments = await _assignmentRepository.GetAssignmentsByEventAsync(request.EventId, cancellationToken);
		var participants = (await _participantRepository.GetAllByEventAsync(request.EventId, cancellationToken)).ToDictionary(p => p.TelegramUserId);
		foreach (var assignment in assignments)
		{
			if (!participants.TryGetValue(assignment.SantaTelegramUserId.ToString(), out var santa) || !participants.TryGetValue(assignment.RecipientTelegramUserId.ToString(), out var recipient))
				continue;

			var keyboard = new InlineKeyboardMarkup(new[]
			{
				new[] { InlineKeyboardButton.WithCallbackData("Уточнити в отримувача", "chat:recipient") },
				new[] { InlineKeyboardButton.WithCallbackData("Написати Secret Santa", "chat:santa") },
				new[] { InlineKeyboardButton.WithCallbackData("Подарунок вже готовий", "gift:ready") },
				new[] { InlineKeyboardButton.WithCallbackData("Ще хочу дещо уточнити", "gift:progress") }
			});

			var message = $"Бюджет узгоджено: {votes.SelectedBudget} грн.\nВаш отримувач: {recipient.DisplayName}\nВішліст: {recipient.WishlistText ?? "Не заповнений"}\n\nВикористайте кнопки нижче для анонімного спілкування та оновлення статусу подарунка.";
			await _telegramSender.SendMessageAsync(santa.TelegramUserId, message, keyboard);
			await _telegramSender.SendMessageAsync(recipient.TelegramUserId, "Жеребкування завершено. Ви можете анонімно уточнити побажання у свого Санти або позначити статус подарунка.", keyboard);
		}

		var current = await _eventRepository.GetByEventIdAsync(request.EventId, cancellationToken);
		if (current != null)
		{
			current.State = "Draft";
			current.BudgetConfirmed = votes.SelectedBudget;
			current.UpdatedAtUtc = DateTime.UtcNow;
			await _eventRepository.UpdateAsync(current, cancellationToken);
		}

		return new CommandResponse { ResponseMessage = $"Усі учасники обрали {votes.SelectedBudget} грн. Жеребкування виконано, Санти отримали дані про своїх отримувачів." };
	}
}
