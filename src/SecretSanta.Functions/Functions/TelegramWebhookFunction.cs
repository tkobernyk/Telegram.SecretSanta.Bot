using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace SecretSanta.Functions.Functions
{
	public class TelegramWebhookFunction
	{
		private readonly ILogger<TelegramWebhookFunction> _logger;

		public TelegramWebhookFunction(ILogger<TelegramWebhookFunction> logger)
		{
			_logger = logger;
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

			// Read body minimally, do not log private content
			using var reader = new StreamReader(req.Body);
			var body = await reader.ReadToEndAsync();

			// TODO: enqueue processing, implement idempotency, validate update_id etc.
			_logger.LogInformation("Received Telegram update (size={Size} bytes).", body?.Length ?? 0);

			var response = req.CreateResponse(HttpStatusCode.OK);
			response.Headers.Add("Content-Type", "application/json; charset=utf-8");
			await response.WriteStringAsync("{ \"received\": true }");
			return response;
		}
	}
}
