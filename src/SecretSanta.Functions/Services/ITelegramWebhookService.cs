using Microsoft.Azure.Functions.Worker.Http;

namespace SecretSanta.Functions.Services
{
	public interface ITelegramWebhookService
	{
		Task<HttpResponseData> HandleAsync(HttpRequestData request);
	}
}
