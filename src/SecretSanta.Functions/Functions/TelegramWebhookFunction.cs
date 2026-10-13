using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SecretSanta.Functions.Services;

namespace SecretSanta.Functions.Functions;

public class TelegramWebhookFunction
{
	readonly ILogger<TelegramWebhookFunction> _logger;
	readonly ITelegramWebhookService _webhookService;

	public TelegramWebhookFunction(ILogger<TelegramWebhookFunction> logger, ITelegramWebhookService webhookService)
	{
		_logger = logger;
		_webhookService = webhookService;
	}

	[Function("TelegramWebhook")]
	public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "telegram/webhook")] HttpRequestData req)
	{
		return await _webhookService.HandleAsync(req);
	}
}
