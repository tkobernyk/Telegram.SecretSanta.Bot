using MediatR;
using SecretSanta.Domain.Models;
using SecretSanta.Domain.Handlers.Common;
using Telegram.Bot.Types.ReplyMarkups;
using SecretSanta.DAL.Abstractions.Repositories;
using SecretSanta.Domain.Helpers;

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

		if (InteractionStateStore.TryGetConversation(telegramUserId, out var state) && state != null)
		{
			if (string.Equals(state.WizardName, "start_event", StringComparison.OrdinalIgnoreCase))
			{
				return await HandleStartWizardAsync(request, state, telegramUserId, text, cancellationToken);
			}

			if (string.Equals(state.WizardName, "create_event_deadline", StringComparison.OrdinalIgnoreCase))
			{
				return await HandleCreateEventDeadlineWizardAsync(request, state, telegramUserId, text, cancellationToken);
			}
		}

		if (string.Equals(text, "start:create", StringComparison.OrdinalIgnoreCase))
		{
			return StartCreateDeadlineSelection(telegramUserId);
		}

		if (string.Equals(text, "start:join", StringComparison.OrdinalIgnoreCase))
		{
			var joinHandler = new StartJoinHandler(_participantRepository, _eventRepository, _userEventSessionRepository);
			return await joinHandler.Handle(
				new StartJoinRequest
				{
					EventId = request.EventId,
					TelegramUserId = telegramUserId,
					JoinCode = string.Empty,
					DisplayName = BuildDisplayName(request.Message, telegramUserId),
					Message = request.Message
				},
				cancellationToken);
		}

		return new StartResponse
		{
			StatusCode = System.Net.HttpStatusCode.OK,
			ResponseMessage = "Виберіть «Створити» або «Приєднатись до події», щоб продовжити реєстрацію.",
			ReplyMarkup = BuildStartInlineKeyboard()
		};
	}

	async Task<StartResponse> HandleStartWizardAsync(StartRequest request, ConversationState state, string telegramUserId, string text, CancellationToken cancellationToken)
	{
		var choice = text.Trim();
		if (state.StepIndex == 0)
		{
			if (choice.Equals("create", StringComparison.OrdinalIgnoreCase) || choice.Equals("new", StringComparison.OrdinalIgnoreCase) || choice.Equals("start:create", StringComparison.OrdinalIgnoreCase))
			{
				return StartCreateDeadlineSelection(telegramUserId);
			}

			if (choice.Equals("join", StringComparison.OrdinalIgnoreCase) || choice.Equals("existing", StringComparison.OrdinalIgnoreCase) || choice.Equals("start:join", StringComparison.OrdinalIgnoreCase))
			{
				var joinHandler = new StartJoinHandler(_participantRepository, _eventRepository, _userEventSessionRepository);
				return await joinHandler.Handle(
					new StartJoinRequest
					{
						EventId = request.EventId,
						TelegramUserId = telegramUserId,
						JoinCode = string.Empty,
						DisplayName = BuildDisplayName(request.Message, telegramUserId),
						Message = request.Message
					},
					cancellationToken);
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

			state.Data.TryGetValue("display_name", out var savedDisplayName);
			var joinHandler = new StartJoinHandler(_participantRepository, _eventRepository, _userEventSessionRepository);
			return await joinHandler.Handle(new StartJoinRequest
			{
				EventId = request.EventId,
				TelegramUserId = telegramUserId,
				JoinCode = joinCode,
				DisplayName = BuildDisplayName(request.Message, telegramUserId, savedDisplayName),
				Message = request.Message
			}, cancellationToken);
		}

		InteractionStateStore.EndConversation(telegramUserId);
		return new StartResponse { StatusCode = System.Net.HttpStatusCode.OK, ResponseMessage = "Спробуйте ще раз: /start" };
	}

	async Task<StartResponse> HandleCreateEventDeadlineWizardAsync(StartRequest request, ConversationState state, string telegramUserId, string text, CancellationToken cancellationToken)
	{
		if (string.Equals(text, "calendar:cancel", StringComparison.OrdinalIgnoreCase))
		{
			InteractionStateStore.EndConversation(telegramUserId);
			return new StartResponse { StatusCode = System.Net.HttpStatusCode.OK, ResponseMessage = "Створення події скасовано. Введіть /start, щоб почати знову." };
		}

		if (text.StartsWith("calendar:month:", StringComparison.OrdinalIgnoreCase))
		{
			var monthText = text["calendar:month:".Length..];
			if (DateOnly.TryParseExact($"{monthText}-01", "yyyy-MM-dd", out var monthDate))
			{
				state.Data["calendar_month"] = monthDate.ToString("yyyy-MM");
				InteractionStateStore.UpdateConversation(telegramUserId, state);
				return new StartResponse
				{
					StatusCode = System.Net.HttpStatusCode.OK,
					ResponseMessage = "Оберіть дату, до якої має бути готовий подарунок:",
					ReplyMarkup = InlineCalendarKeyboard.Create(monthDate, DateOnly.FromDateTime(DateTime.UtcNow))
				};
			}
		}

		if (text.StartsWith("calendar:date:", StringComparison.OrdinalIgnoreCase))
		{
			var dateText = text["calendar:date:".Length..];
			if (DateOnly.TryParseExact(dateText, "yyyy-MM-dd", out var selectedDate))
			{
				var deadlineUtc = DateTime.SpecifyKind(selectedDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
				var createHandler = new StartCreateHandler(_participantRepository, _eventRepository, _userEventSessionRepository);
				return await createHandler.Handle(
					new StartCreateRequest
					{
						EventId = request.EventId,
						TelegramUserId = telegramUserId,
						DisplayName = BuildDisplayName(request.Message, telegramUserId),
						GiftPreparationDeadlineUtc = deadlineUtc,
						Message = request.Message
					},
					cancellationToken);
			}
		}

		state.Data.TryGetValue("calendar_month", out var currentMonthText);
		var currentMonth = DateOnly.TryParseExact($"{currentMonthText}-01", "yyyy-MM-dd", out var parsedMonth)
			? parsedMonth
			: DateOnly.FromDateTime(DateTime.UtcNow);
		return new StartResponse
		{
			StatusCode = System.Net.HttpStatusCode.OK,
			ResponseMessage = "Оберіть дату з календаря кнопками нижче.",
			ReplyMarkup = InlineCalendarKeyboard.Create(currentMonth, DateOnly.FromDateTime(DateTime.UtcNow))
		};
	}

	private static StartResponse StartCreateDeadlineSelection(string telegramUserId)
	{
		var month = DateOnly.FromDateTime(DateTime.UtcNow);
		var state = new ConversationState { WizardName = "create_event_deadline", StepIndex = 0 };
		state.Data["calendar_month"] = month.ToString("yyyy-MM");
		InteractionStateStore.StartConversation(telegramUserId, state);

		return new StartResponse
		{
			StatusCode = System.Net.HttpStatusCode.OK,
			ResponseMessage = "Оберіть дату, до якої має бути готовий подарунок:",
			ReplyMarkup = InlineCalendarKeyboard.Create(month, DateOnly.FromDateTime(DateTime.UtcNow))
		};
	}

	private static string BuildDisplayName(TelegramMessage? message, string telegramUserId, string? fallbackDisplayName = null)
	{
		var firstName = message?.From?.FirstName?.Trim();
		var lastName = message?.From?.LastName?.Trim();
		var username = message?.From?.Username?.Trim();
		var fullName = string.IsNullOrWhiteSpace(lastName)
			? firstName ?? string.Empty
			: string.IsNullOrWhiteSpace(firstName)
				? lastName
				: $"{firstName} {lastName}";

		if (!string.IsNullOrWhiteSpace(fullName) && !string.IsNullOrWhiteSpace(username))
		{
			return $"{fullName} (@{username})";
		}

		if (!string.IsNullOrWhiteSpace(fallbackDisplayName))
		{
			return fallbackDisplayName;
		}

		if (!string.IsNullOrWhiteSpace(fullName))
		{
			return $"{fullName} (@user-{telegramUserId})";
		}

		if (!string.IsNullOrWhiteSpace(username))
		{
			return $"@{username}";
		}

		return $"user-{telegramUserId}";
	}

	private static InlineKeyboardMarkup BuildStartInlineKeyboard()
	{
		return new InlineKeyboardMarkup(
			[
				[InlineKeyboardButton.WithCallbackData("Створити нову подію", "start:create")],
				[InlineKeyboardButton.WithCallbackData("Приєднатись до події за кодом", "start:join")]
			]);
	}
}
