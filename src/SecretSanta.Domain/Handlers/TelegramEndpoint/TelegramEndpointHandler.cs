using MediatR;
using SecretSanta.Domain.Handlers.Commands.Start;
using SecretSanta.Domain.Models;
using SecretSanta.Infrastructure.Telegram;
using System.Net;

namespace SecretSanta.Domain.Handlers.TelegramEndpoint;

public class TelegramEndpointHandler(IMediator _mediator, ITelegramSender _telegramSender) : IRequestHandler<TelegramEndpointRequest, TelegramEndpointResponse>
{
	TelegramEndpointResponse response = default!;

	public async Task<TelegramEndpointResponse> Handle(TelegramEndpointRequest request, CancellationToken cancellationToken)
	{
		var text = request.Message?.Text?.Trim() ?? string.Empty;

		if (string.Equals(text, "/start", StringComparison.OrdinalIgnoreCase))
		{
			var startReq = new StartRequest { EventId = request.EventId, Message = request.Message };
			var startResp = await _mediator.Send(startReq, cancellationToken);
			response = new TelegramEndpointResponse {
				StatusCode = startResp.StatusCode,
				Payload = startResp.Payload,
				ResponseMessage = startResp.ResponseMessage
			};
		}

		try
		{
			var telegramUserId = request.Message?.From?.Id.ToString() ?? string.Empty;
			if(response?.ResponseMessage != null)
			{
				await _telegramSender.SendMessageAsync(telegramUserId, response.ResponseMessage);
			}
		}
		catch (Exception)
		{
			throw new Exception("Failed to send message to Telegram user.");
		}

		return new TelegramEndpointResponse { StatusCode = response!.StatusCode, Payload = new { received = true } };
	}
}
