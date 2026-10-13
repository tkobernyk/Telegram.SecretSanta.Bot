using System.Net;

namespace SecretSanta.Domain.Handlers.TelegramEndpoint;

public class TelegramEndpointResponse
{
	public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
	public object? Payload { get; set; }
	public string? ResponseMessage { get; set; }
}
