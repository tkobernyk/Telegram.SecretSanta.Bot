using MediatR;
using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Enums;
using SecretSanta.Domain.Models;
using SecretSanta.Domain.Helpers;
using Telegram.Bot.Types.ReplyMarkups;

namespace SecretSanta.Domain.Handlers.Commands.Start;

public class StartHandler(IParticipantService _participantService, IEventRepository _eventRepository, IPendingUserInputService _pending) : IRequestHandler<StartRequest, StartResponse>
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

		if (_pending.TryGetConversation(telegramUserId, out var state) && state != null && string.Equals(state.WizardName, "start_event", StringComparison.OrdinalIgnoreCase))
		{
			return await HandleStartWizardAsync(request, state, telegramUserId, text, cancellationToken);
		}

		if (string.Equals(text, "/start", StringComparison.OrdinalIgnoreCase))
		{
			_pending.StartConversation(telegramUserId, new ConversationState { WizardName = "start_event", StepIndex = 0 });
			return new StartResponse
			{
				StatusCode = System.Net.HttpStatusCode.OK,
				Payload = default,
				ResponseMessage = "Виберіть дію:",
				ReplyMarkup = BuildStartInlineKeyboard()
			};

		}

		if (string.Equals(text, "start:create", StringComparison.OrdinalIgnoreCase))
		{
			var createHandler = new StartCreateHandler(_participantService, _eventRepository, _pending);
			return await createHandler.Handle(new StartCreateRequest { EventId = request.EventId, TelegramUserId = telegramUserId, DisplayName = request.Message?.From?.FirstName ?? request.Message?.From?.Username ?? ("user-" + telegramUserId), Message = request.Message }, cancellationToken);
		}

		if (string.Equals(text, "start:join", StringComparison.OrdinalIgnoreCase))
		{
			var joinHandler = new StartJoinHandler(_participantService, _eventRepository, _pending);
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
				var createHandler = new StartCreateHandler(_participantService, _eventRepository, _pending);
				return await createHandler.Handle(new StartCreateRequest { EventId = request.EventId, TelegramUserId = telegramUserId, DisplayName = displayName, Message = request.Message }, cancellationToken);
			}

			if (choice.Equals("join", StringComparison.OrdinalIgnoreCase) || choice.Equals("existing", StringComparison.OrdinalIgnoreCase) || choice.Equals("start:join", StringComparison.OrdinalIgnoreCase))
			{
				var displayName = request.Message?.From?.FirstName ?? request.Message?.From?.Username ?? ("user-" + telegramUserId);
				var joinHandler = new StartJoinHandler(_participantService, _eventRepository, _pending);
				return await joinHandler.Handle(new StartJoinRequest { EventId = request.EventId, TelegramUserId = telegramUserId, JoinCode = string.Empty, DisplayName = displayName, Message = request.Message }, cancellationToken);
			}

			return new StartResponse
			{
				StatusCode = System.Net.HttpStatusCode.OK,
				Payload = default,
				ResponseMessage = "Невірний вибір. Скористайтесь кнопками нижче."
			};
		}

		if (state.StepIndex == 1)
		{
			var joinCode = text.Trim();
			if (string.IsNullOrWhiteSpace(joinCode))
			{
				return new StartResponse { StatusCode = System.Net.HttpStatusCode.BadRequest, ResponseMessage = "Ключ не може бути порожнім." };
			}

			var joinHandler = new StartJoinHandler(_participantService, _eventRepository, _pending);
			return await joinHandler.Handle(new StartJoinRequest
			{
				EventId = request.EventId,
				TelegramUserId = telegramUserId,
				JoinCode = joinCode,
				DisplayName = request.Message?.From?.FirstName ?? request.Message?.From?.Username ?? requestMessageDisplayName(telegramUserId)
			}, cancellationToken);
		}

		_pending.EndConversation(telegramUserId);
		return new StartResponse { StatusCode = System.Net.HttpStatusCode.OK, ResponseMessage = "Спробуйте ще раз: /start" };
	}

	private async Task<StartResponse> HandleCreateAsync(string telegramUserId, CancellationToken cancellationToken)
	{
		var eventEntity = await CreateNewEventAsync(telegramUserId, cancellationToken);
		_pending.EndConversation(telegramUserId);
		return new StartResponse
		{
			StatusCode = System.Net.HttpStatusCode.Created,
			Payload = new { eventId = eventEntity.EventId, joinCode = eventEntity.JoinCode },
			ResponseMessage = $"Ви створили новий Secret Santa. Ключ доступу: {eventEntity.JoinCode}\nПоділіться ним із друзями.\nЗаповніть ваш профіль або побажання кнопками нижче.",
			ReplyMarkup = ProfileWishlistKeyboard.Create()
		};
	}

	private async Task<StartResponse> HandleJoinAsync(string telegramUserId, CancellationToken cancellationToken, ConversationState? state = null)
	{
		if (state == null)
		{
			_pending.StartConversation(telegramUserId, new ConversationState { WizardName = "start_event", StepIndex = 1 });
		}
		else
		{
			state.StepIndex = 1;
			_pending.UpdateConversation(telegramUserId, state);
		}

		return new StartResponse
		{
			StatusCode = System.Net.HttpStatusCode.OK,
			Payload = default,
			ResponseMessage = "Введіть ключ доступу до Secret Santa, який вам дав друг."
		};
	}

	private async Task<Event> CreateNewEventAsync(string telegramUserId, CancellationToken cancellationToken)
	{
		var joinCode = await GenerateUniqueJoinCodeAsync(cancellationToken);
		var eventEntity = new Event
		{
			EventId = Guid.NewGuid().ToString("N"),
			Name = "Secret Santa",
			JoinCode = joinCode,
			OrganizerTelegramId = long.Parse(telegramUserId),
			CreatedByTelegramId = long.Parse(telegramUserId),
			State = EventWorkflowState.Registration,
			CreatedAtUtc = DateTime.UtcNow,
			UpdatedAtUtc = DateTime.UtcNow
		};

		await _eventRepository.CreateAsync(eventEntity, cancellationToken);
		await RegisterParticipantAsync(eventEntity.EventId, telegramUserId, requestMessageDisplayName(telegramUserId), cancellationToken);
		return eventEntity;
	}

	private async Task<string> GenerateUniqueJoinCodeAsync(CancellationToken cancellationToken)
	{
		for (var i = 0; i < 20; i++)
		{
			var code = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
			if (await _eventRepository.GetByJoinCodeAsync(code, cancellationToken) == null)
			{
				return code;
			}
		}

		throw new InvalidOperationException("Unable to generate unique join code for Secret Santa event.");
	}

	private async Task<StartResponse> RegisterParticipantAsync(string eventId, string telegramUserId, string displayName, CancellationToken cancellationToken)
	{
		var existing = await _participantService.GetByTelegramUserIdAsync(eventId, telegramUserId, cancellationToken);
		if (existing != null)
		{
			return new StartResponse
			{
				StatusCode = System.Net.HttpStatusCode.OK,
				Payload = default,
				ResponseMessage = $"Ви вже зареєстровані, {existing.DisplayName}. Перегляньте профіль або оновіть побажання кнопками нижче.",
				ReplyMarkup = ProfileWishlistKeyboard.Create()
			};
		}

		var participant = new ParticipantDto
		{
			EventId = eventId,
			TelegramUserId = telegramUserId,
			DisplayName = displayName,
			WishlistText = string.Empty,
			Hobbies = string.Empty,
			PreferredGiftCategories = string.Empty,
			AvoidCategories = string.Empty,
			PreferredColors = string.Empty,
			OtherPreferences = string.Empty,
			RegistrationStatus = "Registered",
			CreatedAtUtc = DateTime.UtcNow,
			UpdatedAtUtc = DateTime.UtcNow
		};

		await _participantService.CreateAsync(participant, cancellationToken);
		return new StartResponse
		{
			StatusCode = System.Net.HttpStatusCode.Created,
			Payload = default,
			ResponseMessage = $"Дякую, {participant.DisplayName}! Ви успішно зареєстровані. Перегляньте профіль або заповніть побажання кнопками нижче.",
			ReplyMarkup = ProfileWishlistKeyboard.Create()
		};
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
