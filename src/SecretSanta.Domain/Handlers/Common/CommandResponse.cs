using System.Net;

namespace SecretSanta.Domain.Handlers.Common;

public class CommandResponse
{
	public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
	public object? Payload { get; set; }
	public string? ResponseMessage { get; set; }
	public object? ReplyMarkup { get; set; }
}
