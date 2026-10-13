using System.Net;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using MediatR;
using SecretSanta.Domain.Models;
using SecretSanta.Domain.Handlers.TelegramEndpoint;

namespace SecretSanta.Functions.Services;

internal class TelegramWebhookService : ITelegramWebhookService
{
	readonly ILogger<TelegramWebhookService> _logger;
	readonly IMediator _mediator;

	public TelegramWebhookService(ILogger<TelegramWebhookService> logger, IMediator mediator)
	{
		_logger = logger;
		_mediator = mediator;
	}

	private class TelegramUpdate
	{
		public TelegramMessage? Message { get; set; }
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

		if (update?.Message == null || update.Message.From == null)
		{
			_logger.LogInformation("No message or sender in update; ignoring.");
			var respIgnored = req.CreateResponse(HttpStatusCode.OK);
			await respIgnored.WriteStringAsync("{ \"received\": true }");
			return respIgnored;
		}

		var eventId = Environment.GetEnvironmentVariable("App__EventId") ?? "default-event";

		// Build MediatR request and delegate handling to handlers
		var request = new TelegramEndpointRequest
		{
			EventId = eventId,
			Message = new TelegramMessage
			{
				MessageId = update.Message.MessageId,
				From = new TelegramUser
				{
					Id = update.Message.From.Id,
					FirstName = update.Message.From.FirstName,
					LastName = update.Message.From.LastName,
					Username = update.Message.From.Username
				},
				Text = update.Message.Text
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
