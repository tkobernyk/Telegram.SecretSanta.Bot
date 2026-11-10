using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using MediatR;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Models;
using SecretSanta.Domain.Handlers.TelegramEndpoint;

namespace SecretSanta.Functions.Services;

internal class TelegramWebhookService : ITelegramWebhookService
{
	readonly ILogger<TelegramWebhookService> _logger;
	readonly IMediator _mediator;
	readonly IPendingUserInputService _pending;

	public TelegramWebhookService(ILogger<TelegramWebhookService> logger, IMediator mediator, IPendingUserInputService pending)
	{
		_logger = logger;
		_mediator = mediator;
		_pending = pending;
	}

	private class TelegramUpdate
	{
		[JsonPropertyName("message")]
		public TelegramMessage? Message { get; set; }
		[JsonPropertyName("callback_query")]
		public TelegramCallbackQuery? CallbackQuery { get; set; }
	}

	private class TelegramCallbackQuery
	{
		[JsonPropertyName("from")]
		public TelegramUser? From { get; set; }
		[JsonPropertyName("data")]
		public string? Data { get; set; }
		[JsonPropertyName("id")]
		public string? Id { get; set; }
	}

	public async Task<HttpResponseData> HandleAsync(HttpRequestData req)
	{
		var responseUnauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
		string headerName = "X-Telegram-Bot-Api-Secret-Token";

		if (!req.Headers.TryGetValues(headerName, out var values))
		{
			_logger.LogWarning("Missing webhook secret header.");
			responseUnauthorized.Headers.Add("Content-Type", "text/plain; charset=utf-8");
			await responseUnauthorized.WriteStringAsync("Unauthorized");
			return responseUnauthorized;
		}

		var provided = Enumerable.FirstOrDefault(values);
		var expected = Environment.GetEnvironmentVariable("Telegram__WebhookSecret") ?? string.Empty;

		var providedBytes = Encoding.UTF8.GetBytes(provided ?? string.Empty);
		var expectedBytes = Encoding.UTF8.GetBytes(expected);
		bool ok = false;
		try
		{
			if (providedBytes.Length == expectedBytes.Length)
			{
				ok = CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
			}
		}
		finally { }

		if (!ok)
		{
			_logger.LogWarning("Invalid webhook secret provided.");
			responseUnauthorized.Headers.Add("Content-Type", "text/plain; charset=utf-8");
			await responseUnauthorized.WriteStringAsync("Unauthorized");
			return responseUnauthorized;
		}

		using var reader = new StreamReader(req.Body);
		var body = await reader.ReadToEndAsync();

		if (string.IsNullOrWhiteSpace(body))
		{
			_logger.LogWarning("Empty update payload.");
			var respEmpty = req.CreateResponse(HttpStatusCode.BadRequest);
			await respEmpty.WriteStringAsync("Empty payload");
			return respEmpty;
		}

		TelegramUpdate? update = null;
		try
		{
			update = JsonSerializer.Deserialize<TelegramUpdate>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to parse Telegram update JSON.");
		}

		var message = update?.Message;
		var callback = update?.CallbackQuery;
	var sender = callback?.From ?? message?.From;
		if (sender == null)
		{
			_logger.LogInformation("No message or sender in update; ignoring.");
			var respIgnored = req.CreateResponse(HttpStatusCode.OK);
			await respIgnored.WriteStringAsync("{ \"received\": true }");
			return respIgnored;
		}

		var defaultEventId = Environment.GetEnvironmentVariable("App__EventId") ?? "default-event";
		var eventId = defaultEventId;
		var persistedEventId = await _pending.GetActiveEventIdAsync(sender.Id.ToString());
		if (!string.IsNullOrWhiteSpace(persistedEventId))
		{
			eventId = persistedEventId;
		}

		var request = new TelegramEndpointRequest
		{
			EventId = eventId,
			Message = new TelegramMessage
			{
				MessageId = message?.MessageId ?? 0,
				From = new TelegramUser
				{
					Id = sender.Id,
					FirstName = sender.FirstName,
					LastName = sender.LastName,
					Username = sender.Username
				},
				Text = message?.Text ?? callback?.Data,
				CallbackData = callback?.Data,
				CallbackQueryId = callback?.Id
			}
		};

		TelegramEndpointResponse? result = null;
		try
		{
			result = await _mediator.Send(request);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling telegram update via MediatR");
		}

		// Handlers are responsible for sending any messages to Telegram via ITelegramSender.
		// Webhook should only acknowledge receipt to Telegram's servers.
		var response = req.CreateResponse(HttpStatusCode.OK);
		response.Headers.Add("Content-Type", "application/json; charset=utf-8");
		await response.WriteStringAsync("{ \"received\": true }");
		return response;
	}
}
