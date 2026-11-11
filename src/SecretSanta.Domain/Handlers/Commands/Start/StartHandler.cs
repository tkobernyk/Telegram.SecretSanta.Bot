using MediatR;
using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Enums;
using SecretSanta.Domain.Models;
using SecretSanta.Domain.Mappings;
using SecretSanta.Domain.Helpers;
using SecretSanta.Domain.Handlers.Common;
using Telegram.Bot.Types.ReplyMarkups;
using SecretSanta.DAL.Abstractions.Repositories;

namespace SecretSanta.Domain.Handlers.Commands.Start;

public class StartHandler(IParticipantRepository _participantRepository, IEventRepository _eventRepository, IUserEventSessionRepository _userEventSessionRepository) : IRequestHandler<StartRequest, StartResponse>
{
	public async Task<StartResponse> Handle(StartRequest request, CancellationToken cancellationToken)
	{
		var telegramUserId = request.Message?.From?.Id.ToString() ?? string.Empty;
		var text = request.Message?.Text?.Trim() ?? string.Empty;
		var callbackData = request.Message?.CallbackData?.Trim() ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(callbackData))
		{
			text = callbackData;
		}

		if (string.IsNullOrEmpty(telegramUserId))
		{
			return new StartResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, Payload = new { text = "Missing user id" } };
		}

		if (string.Equals(text, "/start", StringComparison.OrdinalIgnoreCase))
		{
			InteractionStateStore.StartConversation(telegramUserId, new ConversationState { WizardName = "start_event", StepIndex = 0 });
			return new StartResponse
			{
				StatusCode = System.Net.HttpStatusCode.OK,
				Payload = default,
				ResponseMessage = "Виберіть дію:",
				ReplyMarkup = BuildStartInlineKeyboard()
			};
		}

		if (InteractionStateStore.TryGetConversation(telegramUserId, out var state) && state != null && string.Equals(state.WizardName, "start_event", StringComparison.OrdinalIgnoreCase))
		{
			return await HandleStartWizardAsync(request, state, telegramUserId, text, cancellationToken);
		}

		if (string.Equals(text, "start:create", StringComparison.OrdinalIgnoreCase))
		{
			var createHandler = new StartCreateHandler(_participantRepository, _eventRepository, _userEventSessionRepository);
			return await createHandler.Handle(new StartCreateRequest { EventId = request.EventId, TelegramUserId = telegramUserId, DisplayName = request.Message?.From?.FirstName ?? request.Message?.From?.Username ?? ("user-" + telegramUserId), Message = request.Message }, cancellationToken);
		}

		if (string.Equals(text, "start:join", StringComparison.OrdinalIgnoreCase))
		{
			var joinHandler = new StartJoinHandler(_participantRepository, _eventRepository, _userEventSessionRepository);
			return await joinHandler.Handle(new StartJoinRequest { EventId = request.EventId, TelegramUserId = telegramUserId, JoinCode = string.Empty, DisplayName = request.Message?.From?.FirstName ?? request.Message?.From?.Username ?? ("user-" + telegramUserId), Message = request.Message }, cancellationToken);
		}

		return new StartResponse
		{
			StatusCode = System.Net.HttpStatusCode.OK,
			ResponseMessage = "Виберіть «Створити» або «Приєднатись до події», щоб продовжити реєстрацію.",
			ReplyMarkup = BuildStartInlineKeyboard()
		};
	}

	private async Task<StartResponse> HandleStartWizardAsync(StartRequest request, ConversationState state, string telegramUserId, string text, CancellationToken cancellationToken)
	{
		var choice = text.Trim();
		if (state.StepIndex == 0)
		{
			if (choice.Equals("create", StringComparison.OrdinalIgnoreCase) || choice.Equals("new", StringComparison.OrdinalIgnoreCase) || choice.Equals("start:create", StringComparison.OrdinalIgnoreCase))
			{
				var displayName = request.Message?.From?.FirstName ?? request.Message?.From?.Username ?? ("user-" + telegramUserId);
				var createHandler = new StartCreateHandler(_participantRepository, _eventRepository, _userEventSessionRepository);
				return await createHandler.Handle(new StartCreateRequest { EventId = request.EventId, TelegramUserId = telegramUserId, DisplayName = displayName, Message = request.Message }, cancellationToken);
			}

			if (choice.Equals("join", StringComparison.OrdinalIgnoreCase) || choice.Equals("existing", StringComparison.OrdinalIgnoreCase) || choice.Equals("start:join", StringComparison.OrdinalIgnoreCase))
			{
				var displayName = request.Message?.From?.FirstName ?? request.Message?.From?.Username ?? ("user-" + telegramUserId);
				var joinHandler = new StartJoinHandler(_participantRepository, _eventRepository, _userEventSessionRepository);
				return await joinHandler.Handle(new StartJoinRequest { EventId = request.EventId, TelegramUserId = telegramUserId, JoinCode = string.Empty, DisplayName = displayName, Message = request.Message }, cancellationToken);
			}

			return new StartResponse
			{
				StatusCode = System.Net.HttpStatusCode.OK,
				Payload = default,
				ResponseMessage = "Невірний вибір. Скористайтесь кнопками нижче.",
				ReplyMarkup = BuildStartInlineKeyboard()
			};
		}

		if (state.StepIndex == 1)
		{
			var joinCode = text.Trim();
			if (string.IsNullOrWhiteSpace(joinCode))
			{
				return new StartResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Ключ не може бути порожнім." };
			}

			var joinHandler = new StartJoinHandler(_participantRepository, _eventRepository, _userEventSessionRepository);
			return await joinHandler.Handle(new StartJoinRequest
			{
				EventId = request.EventId,
				TelegramUserId = telegramUserId,
				JoinCode = joinCode,
				DisplayName = request.Message?.From?.FirstName ?? request.Message?.From?.Username ?? requestMessageDisplayName(telegramUserId)
			}, cancellationToken);
		}

		InteractionStateStore.EndConversation(telegramUserId);
		return new StartResponse { StatusCode = System.Net.HttpStatusCode.OK, ResponseMessage = "Спробуйте ще раз: /start" };
	}

	private static InlineKeyboardMarkup BuildStartInlineKeyboard()
	{
		return new InlineKeyboardMarkup(
			[
				[InlineKeyboardButton.WithCallbackData("Створити нову подію", "start:create")],
				[InlineKeyboardButton.WithCallbackData("Приєднатись до події за кодом", "start:join")]
			]);
	}

















































































































































































	private static string requestMessageDisplayName(string telegramUserId)
	{
		return $"user-{telegramUserId}";
	}
}
