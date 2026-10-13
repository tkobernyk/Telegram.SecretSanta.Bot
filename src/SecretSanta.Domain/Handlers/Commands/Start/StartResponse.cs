using System.Net;

namespace SecretSanta.Domain.Handlers.Commands.Start;

public class StartResponse
{
	public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
	public object? Payload { get; set; }
	public string? ResponseMessage { get; set; }
}
