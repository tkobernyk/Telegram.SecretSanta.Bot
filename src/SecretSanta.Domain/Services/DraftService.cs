using SecretSanta.Domain.Abstractions;
using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;

namespace SecretSanta.Domain.Services;

public class DraftService : IDraftService
{
	readonly IParticipantRepository _participantRepo;
	readonly IAssignmentRepository _assignmentRepo;
	readonly IEventRepository _eventRepo;

	public DraftService(IParticipantRepository participantRepo, IAssignmentRepository assignmentRepo, IEventRepository eventRepo)
	{
		_participantRepo = participantRepo;
		_assignmentRepo = assignmentRepo;
		_eventRepo = eventRepo;
	}

	public async Task StartDraftAsync(string eventId, long initiatorTelegramId, CancellationToken cancellationToken = default)
	{
		var ev = await _eventRepo.GetByEventIdAsync(eventId, cancellationToken);
		if (ev != null)
		{
			ev.State = "Draft";
			ev.UpdatedAtUtc = DateTime.UtcNow;
			await _eventRepo.UpdateAsync(ev, cancellationToken);
		}

			// load participants
			var participants = (await _participantRepo.GetAllByEventAsync(eventId, cancellationToken)).ToList();
		if (participants.Count < 1) throw new InvalidOperationException("Not enough participants to run draft.");

		// simple derangement algorithm: shuffle and ensure no self-assign; retry up to N times
		var rng = new Random();
		var ids = participants.Select(p => p.TelegramUserId).ToList();
		int attempts = 0;
		List<string> recipients = [];
		while (attempts < 50)
		{
			attempts++;
			recipients = [.. ids.OrderBy(_ => rng.Next())];
			bool ok = true;
			for (int i = 0; i < ids.Count; i++) if (ids[i] == recipients[i]) { ok = false; break; }
			if (ok) break;
		}

		if (recipients == null || recipients.Count != ids.Count) throw new InvalidOperationException("Failed to compute assignments without self-assigns.");

		var draftRunId = Guid.NewGuid().ToString();
		var assignments = new List<Assignment>();
		for (int i = 0; i < ids.Count; i++)
		{
			assignments.Add(new Assignment { EventId = eventId, DraftRunId = draftRunId, SantaTelegramUserId = long.Parse(ids[i]), RecipientTelegramUserId = long.Parse(recipients[i]) });
		}

		await _assignmentRepo.CreateAssignmentsAsync(assignments, cancellationToken);
	}
}
