using MediatR;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.Infrastructure.Telegram;

namespace SecretSanta.Domain.Handlers.Commands.Voting;

public class StartVoteSessionHandler : IRequestHandler<StartVoteSessionRequest, CommandResponse>
{
	private readonly IVoteService _voteService;
	private readonly SecretSanta.DAL.Abstractions.IEventRepository _eventRepo;
	private readonly SecretSanta.DAL.Abstractions.IParticipantRepository _participantRepo;
	private readonly ITelegramSender _telegramSender;

	public StartVoteSessionHandler(IVoteService voteService, SecretSanta.DAL.Abstractions.IEventRepository eventRepo, SecretSanta.DAL.Abstractions.IParticipantRepository participantRepo, ITelegramSender telegramSender)
	{
		_voteService = voteService;
		_eventRepo = eventRepo;
		_participantRepo = participantRepo;
		_telegramSender = telegramSender;
	}

	public async Task<CommandResponse> Handle(StartVoteSessionRequest request, CancellationToken cancellationToken)
	{
		// verify organizer
		var ev = await _eventRepo.GetByEventIdAsync(request.EventId, cancellationToken);
		if (ev == null) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Event not found." };
		if (ev.OrganizerTelegramId != request.InitiatorTelegramId) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.Forbidden, ResponseMessage = "Only organizer may start voting." };
		var registered = (await _participantRepo.GetAllByEventAsync(request.EventId, cancellationToken)).Count();
		if (registered < 2) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Для голосування потрібно щонайменше два учасники." };
		if (ev.State != "Registration" && ev.State != "BudgetDiscussion") return new CommandResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Голосування можна запустити лише після реєстрації або обговорення бюджету." };

		request.Session.EventId = request.EventId;
		await _voteService.CreateVoteSessionAsync(request.EventId, request.Session, cancellationToken);
		ev.CurrentVoteSessionId = request.Session.Id;
		ev.State = "Voting";
		await _eventRepo.UpdateAsync(ev, cancellationToken);
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
