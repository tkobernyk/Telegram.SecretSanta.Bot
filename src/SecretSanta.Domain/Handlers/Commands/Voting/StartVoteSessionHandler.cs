using MediatR;
using SecretSanta.DAL.Abstractions.Repositories;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Enums;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.Domain.Handlers.Commands.Workflow;
using SecretSanta.Infrastructure.Telegram;

namespace SecretSanta.Domain.Handlers.Commands.Voting;

public class StartVoteSessionHandler : IRequestHandler<StartVoteSessionRequest, CommandResponse>
{
	private readonly IVoteService _voteService;
	private readonly IEventRepository _eventRepo;
	private readonly IVoteRepository _voteServiceRepository;
	private readonly IParticipantRepository _participantRepo;
	private readonly ITelegramSender _telegramSender;
	private readonly IMediator _mediator;

	public StartVoteSessionHandler(IVoteService voteService, IEventRepository eventRepo, IVoteRepository voteRepository, IParticipantRepository participantRepo, ITelegramSender telegramSender, IMediator mediator)
	{
		_voteService = voteService;
		_eventRepo = eventRepo;
		_voteServiceRepository = voteRepository;
		_participantRepo = participantRepo;
		_telegramSender = telegramSender;
		_mediator = mediator;
	}

	public async Task<CommandResponse> Handle(StartVoteSessionRequest request, CancellationToken cancellationToken)
	{
		// verify organizer
		var ev = await _eventRepo.GetByEventIdAsync(request.EventId, cancellationToken);
		if (ev == null) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Event not found." };
		if (ev.OrganizerTelegramId != request.InitiatorTelegramId) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.Forbidden, ResponseMessage = "Only organizer may start voting." };
		var registered = (await _participantRepo.GetAllByEventAsync(request.EventId, cancellationToken)).Count();
		if (registered < 2) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Для голосування потрібно щонайменше два учасники." };
		if (string.Equals(ev.State, EventWorkflowState.Voting, StringComparison.OrdinalIgnoreCase))
			return new CommandResponse { StatusCode = System.Net.HttpStatusCode.Conflict, ResponseMessage = "Голосування за бюджет уже триває." };
		if (ev.State is EventWorkflowState.Draft or EventWorkflowState.Preparing or EventWorkflowState.AwaitingDelivery or EventWorkflowState.Done or EventWorkflowState.Completed or EventWorkflowState.Cancelled)
			return new CommandResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Для цієї події голосування за бюджет уже недоступне." };

		request.Session.EventId = request.EventId;
		var session = _voteService.PrepareVoteSession(request.Session);
		var transitionResponse = await _mediator.Send(new ApplyEventTransitionRequest
		{
			EventId = request.EventId,
			Trigger = EventWorkflowTrigger.VoteSessionStarted,
			ChangedByTelegramId = request.InitiatorTelegramId,
			Note = "Vote session started"
		}, cancellationToken);
		if (transitionResponse.StatusCode != System.Net.HttpStatusCode.OK)
		{
			return transitionResponse;
		}

		await _voteServiceRepository.CreateVoteSessionAsync(session, cancellationToken);
		var refreshedEvent = await _eventRepo.GetByEventIdAsync(request.EventId, cancellationToken);
		if (refreshedEvent != null)
		{
			refreshedEvent.CurrentVoteSessionId = session.Id;
			await _eventRepo.UpdateAsync(refreshedEvent, cancellationToken);
		}
		var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
		{
			new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("250 грн", "budget:250"), Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("500 грн", "budget:500") },
			new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("750 грн", "budget:750"), Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("1000 грн", "budget:1000") },
			new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("1500 грн", "budget:1500") }
		});
		foreach (var participant in await _participantRepo.GetAllByEventAsync(request.EventId, cancellationToken))
		{
			await _telegramSender.SendMessageAsync(participant.TelegramUserId, "Оберіть спільний бюджет подарунка:", keyboard);
		}
		return new CommandResponse
		{
			StatusCode = System.Net.HttpStatusCode.OK,
			ResponseMessage = "Голосування за бюджет розпочато."
		};
	}

}
