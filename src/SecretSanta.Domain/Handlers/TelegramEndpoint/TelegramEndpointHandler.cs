using MediatR;
using SecretSanta.Domain.Handlers.Commands.Start;
using SecretSanta.Domain.Handlers.Commands.Profile;
using SecretSanta.Domain.Handlers.Commands.Wishlist;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.Infrastructure.Telegram;
using SecretSanta.Domain.Enums;
using SecretSanta.Domain.Helpers;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Handlers.Commands.Participant;
using SecretSanta.Domain.Handlers.Commands.Voting;
using SecretSanta.DAL.Entities;
using SecretSanta.DAL.Abstractions;

namespace SecretSanta.Domain.Handlers.TelegramEndpoint;

public class TelegramEndpointHandler(IMediator _mediator, ITelegramSender _telegramSender, IPendingUserInputService _pending, IEventRepository _eventRepository, IParticipantRepository _participantRepository) : IRequestHandler<TelegramEndpointRequest, TelegramEndpointResponse>
{
	public async Task<TelegramEndpointResponse> Handle(TelegramEndpointRequest request, CancellationToken cancellationToken)
	{
		var text = request.Message?.Text?.Trim() ?? string.Empty;
		var callbackData = request.Message?.CallbackData?.Trim() ?? string.Empty;
		var telegramUserId = request.Message?.From?.Id.ToString() ?? string.Empty;
		CommandResponse? cmdResp = null;
		TelegramEndpointResponse? response = null;
		if (string.Equals(text, "/start_vote", StringComparison.OrdinalIgnoreCase))
		{
			cmdResp = await _mediator.Send(new StartVoteSessionRequest
			{
				EventId = request.EventId,
				InitiatorTelegramId = request.Message?.From?.Id ?? 0,
				Session = new VoteSession { EventId = request.EventId }
			}, cancellationToken);
		}
		else if (!string.IsNullOrEmpty(telegramUserId) && _pending.TryGetConversation(telegramUserId, out var deliveryState) && deliveryState?.WizardName == "delivery_info" && !string.IsNullOrWhiteSpace(text))
		{
			var ev = await _eventRepository.GetByEventIdAsync(request.EventId, cancellationToken);
			if (ev == null || ev.OrganizerTelegramId.ToString() != telegramUserId)
			{
				cmdResp = new CommandResponse { StatusCode = System.Net.HttpStatusCode.Forbidden, ResponseMessage = "Лише організатор може вказати місце й час вручення." };
			}
			else
			{
				ev.DeliveryLocation = text;
				ev.DeliveryMessageSentUtc = DateTime.UtcNow;
				ev.State = "Done";
				ev.UpdatedAtUtc = DateTime.UtcNow;
				await _eventRepository.UpdateAsync(ev, cancellationToken);
				_pending.EndConversation(telegramUserId);
				var participants = await _participantRepository.GetAllByEventAsync(request.EventId, cancellationToken);
				foreach (var participant in participants)
				{
					await _telegramSender.SendMessageAsync(participant.TelegramUserId, $"Місце й час вручення Secret Santa: {text}");
				}
				cmdResp = new CommandResponse { ResponseMessage = "Дякую! Місце й час вручення надіслані всім учасникам." };
			}
		}
		if (!string.IsNullOrEmpty(telegramUserId) && _pending.TryGetConversation(telegramUserId, out var startState) && startState != null && string.Equals(startState.WizardName, "start_event", StringComparison.OrdinalIgnoreCase))
		{
			var startReq = new StartRequest { EventId = request.EventId, Message = request.Message };
			var startResp = await _mediator.Send(startReq, cancellationToken);
			cmdResp = startResp;
		}
		else if (!string.IsNullOrWhiteSpace(callbackData))
		{
			if (callbackData == "vote:start")
			{
				cmdResp = await _mediator.Send(new StartVoteSessionRequest
				{
					EventId = request.EventId,
					InitiatorTelegramId = request.Message?.From?.Id ?? 0,
					Session = new VoteSession { EventId = request.EventId }
				}, cancellationToken);
			}
			else if (callbackData.StartsWith("budget:", StringComparison.OrdinalIgnoreCase))
			{
				if (long.TryParse(callbackData["budget:".Length..], out var budget))
				{
					cmdResp = await _mediator.Send(new SubmitBudgetVoteRequest
					{
						EventId = request.EventId,
						TelegramUserId = request.Message?.From?.Id ?? 0,
						Budget = budget
					}, cancellationToken);
				}
			}
			else if (callbackData == "gift:ready" || callbackData == "gift:progress")
			{
				cmdResp = await _mediator.Send(new MarkReadyRequest
				{
					EventId = request.EventId,
					TelegramUserId = request.Message?.From?.Id ?? 0,
					GiftStatus = callbackData == "gift:ready" ? "Ready" : "InProgress"
				}, cancellationToken);
			}
			else if (callbackData == "chat:recipient" || callbackData == "chat:santa")
			{
				cmdResp = await _mediator.Send(new Commands.Conversation.StartConversationRequest
				{
					EventId = request.EventId,
					InitiatorTelegramUserId = telegramUserId,
					StartTarget = callbackData == "chat:recipient" ? "Recipient" : "Santa"
				}, cancellationToken);
			}
			else if (callbackData is "start:create" or "start:join")
			{
				var startReq = new StartRequest { EventId = request.EventId, Message = request.Message };
				cmdResp = await _mediator.Send(startReq, cancellationToken);
			}
		}

		if (CommandHelper.TryMapCommand(text, out var command))
		{
			switch (command)
			{
				case CommandType.Start:
				{
					var startReq = new StartRequest { EventId = request.EventId, Message = request.Message };
					var startResp = await _mediator.Send(startReq, cancellationToken);
					cmdResp = startResp;
					break;
				}
				case CommandType.Profile:
				{
					var profileReq = new ProfileRequest { EventId = request.EventId, Message = request.Message };
					var profileResp = await _mediator.Send(profileReq, cancellationToken);
					cmdResp = profileResp;
					break;
				}
				case CommandType.Wishlist:
				{
					var wishlistReq = new WishlistRequest { EventId = request.EventId, Message = request.Message };
					var wishlistResp = await _mediator.Send(wishlistReq, cancellationToken);
					cmdResp = wishlistResp;
					break;
				}
				case CommandType.Cancel:
				{
					// cancel active conversation for user (either santa or recipient)
					var cancelReq = new Commands.Conversation.CancelConversationRequest { EventId = request.EventId, InitiatorTelegramUserId = request.Message?.From?.Id.ToString() ?? string.Empty };
					await _mediator.Send(cancelReq, cancellationToken);
					cmdResp = new CommandResponse { StatusCode = System.Net.HttpStatusCode.OK, ResponseMessage = "Діалог скасовано." };
					break;
				}
				case CommandType.AskSanta:
				{
					var askSantaReq = new Commands.Conversation.StartConversationRequest { EventId = request.EventId, InitiatorTelegramUserId = request.Message?.From?.Id.ToString() ?? string.Empty, StartTarget = "Santa" };
					var askSantaResp = await _mediator.Send(askSantaReq, cancellationToken);
					cmdResp = askSantaResp;
					break;
				}
				case CommandType.AskRecipient:
				{
					var askRecReq = new Commands.Conversation.StartConversationRequest { EventId = request.EventId, InitiatorTelegramUserId = request.Message?.From?.Id.ToString() ?? string.Empty, StartTarget = "Recipient" };
					var askRecResp = await _mediator.Send(askRecReq, cancellationToken);
					cmdResp = askRecResp;
					break;
				}
				case CommandType.StartVote:
				{
					// organizer-only: start anonymous conversation between santa and recipient (for testing/admin) - only if organizer triggers
					var organizerId = request.Message?.From?.Id.ToString() ?? string.Empty;
					var ev = await _mediator.Send(new StartRequest { EventId = request.EventId, Message = request.Message }, cancellationToken);
					// Not exposing santa identity in handler; this command should be protected by organizer check if used to start conversation between two specified users. For now return NotImplemented message.

					cmdResp = new CommandResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Команда AskSanta доступна тільки організатору і реалізується окремим адмін-інструментом." };
					break;
				}
				default:
					response = new TelegramEndpointResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, Payload = new { error = "Unknown command" } };
					break;
			}
		}
		else
		{
			// first give general conversational handlers a chance (incoming message flows)
			var incomingReq = new SecretSanta.Domain.Handlers.Commands.Conversation.IncomingMessageRequest { EventId = request.EventId, Message = request.Message };
			var incomingResp = await _mediator.Send(incomingReq, cancellationToken);
			if (incomingResp?.ResponseMessage != null)
			{
				cmdResp = incomingResp;
			}
			else
			{
				// fallback to wishlist flow
				var wishlistReq = new WishlistRequest { EventId = request.EventId, Message = request.Message };
				var wishlistResp = await _mediator.Send(wishlistReq, cancellationToken);
				if (wishlistResp?.ResponseMessage != null)
				{
					cmdResp = wishlistResp;
				}
			}
		}

		if (cmdResp != null)
		{
			response = new TelegramEndpointResponse {
				StatusCode = cmdResp.StatusCode,
				Payload = cmdResp.Payload,
				ResponseMessage = cmdResp.ResponseMessage,
				ReplyMarkup = cmdResp.ReplyMarkup
			};
		}

		try
		{
			var recipientTelegramUserId = request.Message?.From?.Id.ToString() ?? string.Empty;
			if (response?.ResponseMessage != null)
			{
				await _telegramSender.SendMessageAsync(recipientTelegramUserId, response.ResponseMessage, response.ReplyMarkup as Telegram.Bot.Types.ReplyMarkups.ReplyMarkup);
			}
		}
		catch (Exception)
		{
			throw new Exception("Failed to send message to Telegram user.");
		}

		return new TelegramEndpointResponse { StatusCode = response!.StatusCode, Payload = new { received = true } };
	}
}
