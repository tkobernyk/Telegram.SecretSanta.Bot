using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Models;

namespace SecretSanta.Functions.Functions;

public class TelegramWebhookFunction
{
	readonly ILogger<TelegramWebhookFunction> _logger;
	readonly IParticipantService _participantService;

	public TelegramWebhookFunction(ILogger<TelegramWebhookFunction> logger, IParticipantService participantService)
	{
		_logger = logger;
		_participantService = participantService;
	}

	private class TelegramUpdate
	{
		public TelegramMessage? Message { get; set; }
	}

	private class TelegramMessage
	{
		public long MessageId { get; set; }
		public TelegramUser? From { get; set; }
		public string? Text { get; set; }
	}

	private class TelegramUser
	{
		public long Id { get; set; }
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? Username { get; set; }
	}

	[Function("TelegramWebhook")]
	public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "telegram/webhook")] HttpRequestData req)
	{
		// Validate secret header
		var responseUnauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
		string headerName = "X-Telegram-Bot-Api-Secret-Token";

		if (!req.Headers.TryGetValues(headerName, out var values))
		{
			_logger.LogWarning("Missing webhook secret header.");
			responseUnauthorized.Headers.Add("Content-Type", "text/plain; charset=utf-8");
			await responseUnauthorized.WriteStringAsync("Unauthorized");
			return responseUnauthorized;
		}

		var provided = System.Linq.Enumerable.FirstOrDefault(values);
		var expected = Environment.GetEnvironmentVariable("Telegram__WebhookSecret") ?? string.Empty;

		// Constant-time comparison
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
		finally
		{
			// zeroize arrays if needed
		}

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

		var text = update.Message.Text?.Trim() ?? string.Empty;
		var telegramUserId = update.Message.From.Id.ToString();
		var eventId = Environment.GetEnvironmentVariable("App__EventId") ?? "default-event";

		if (text == "/start")
		{
			// Check existing
			var existing = await _participantService.GetByTelegramUserIdAsync(eventId, telegramUserId);
			if (existing != null)
			{
				// Reply with existing profile summary (Ukrainian)
				var reply = new { text = $"Вітаю, {existing.DisplayName}! Ви вже зареєстровані." };
				var resp = req.CreateResponse(HttpStatusCode.OK);
				resp.Headers.Add("Content-Type", "application/json; charset=utf-8");
				await resp.WriteStringAsync(JsonSerializer.Serialize(reply));
				return resp;
			}

			// Create new participant with minimal profile
			var participant = new ParticipantDto
			{
				EventId = eventId,
				TelegramUserId = telegramUserId,
				DisplayName = update.Message.From.FirstName ?? update.Message.From.Username ?? ("user-" + telegramUserId),
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

			await _participantService.CreateAsync(participant);

			var reply2 = new { text = $"Дякую, {participant.DisplayName}! Ви успішно зареєстровані. Використайте /profile щоб переглянути анкету." };
			var resp2 = req.CreateResponse(HttpStatusCode.OK);
			resp2.Headers.Add("Content-Type", "application/json; charset=utf-8");
			await resp2.WriteStringAsync(JsonSerializer.Serialize(reply2));
			return resp2;
		}

		// Default response: acknowledge
		var response = req.CreateResponse(HttpStatusCode.OK);
		response.Headers.Add("Content-Type", "application/json; charset=utf-8");
		await response.WriteStringAsync("{ \"received\": true }");
		return response;
	}
}
