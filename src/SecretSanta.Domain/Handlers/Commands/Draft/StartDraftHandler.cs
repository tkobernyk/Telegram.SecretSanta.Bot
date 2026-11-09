using MediatR;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.DAL.Abstractions;

namespace SecretSanta.Domain.Handlers.Commands.Draft;

public class StartDraftHandler : IRequestHandler<StartDraftRequest, CommandResponse>
{
	readonly IDraftService _draftService;
	readonly IEventRepository _eventRepo;

	public StartDraftHandler(IDraftService draftService, IEventRepository eventRepo)
	{
		_draftService = draftService;
		_eventRepo = eventRepo;
	}

	public async Task<CommandResponse> Handle(StartDraftRequest request, CancellationToken cancellationToken)
	{
		var ev = await _eventRepo.GetByEventIdAsync(request.EventId, cancellationToken);
		if (ev == null) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.NotFound, ResponseMessage = "Event not found." };
		if (ev.OrganizerTelegramId != request.InitiatorTelegramId) return new CommandResponse { StatusCode = System.Net.HttpStatusCode.Forbidden, ResponseMessage = "Only organizer may start draft." };

		await _draftService.StartDraftAsync(request.EventId, request.InitiatorTelegramId, cancellationToken);
		return new CommandResponse { StatusCode = System.Net.HttpStatusCode.OK, ResponseMessage = "Жеребкування виконано та призначення опубліковано." };
	}
}
