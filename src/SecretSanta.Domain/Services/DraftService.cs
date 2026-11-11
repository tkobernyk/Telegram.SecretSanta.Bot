using SecretSanta.Domain.Abstractions;
using SecretSanta.DAL.Entities;

namespace SecretSanta.Domain.Services;

public class DraftService : IDraftService
{
	public IReadOnlyList<Assignment> BuildAssignments(string eventId, IReadOnlyList<string> participantTelegramUserIds)
	{
		if (participantTelegramUserIds.Count < 1)
		{
			throw new InvalidOperationException("Not enough participants to run draft.");
		}

		var rng = new Random();
		var ids = participantTelegramUserIds.ToList();
		List<string> recipients = [];
		for (var attempts = 0; attempts < 50; attempts++)
		{
			recipients = [.. ids.OrderBy(_ => rng.Next())];
			var valid = true;
			for (var index = 0; index < ids.Count; index++)
			{
				if (ids[index] == recipients[index])
				{
					valid = false;
					break;
				}
			}

			if (valid)
			{
				break;
			}
		}

		if (recipients.Count != ids.Count)
		{
			throw new InvalidOperationException("Failed to compute assignments without self-assigns.");
		}

		var draftRunId = Guid.NewGuid().ToString();
		var assignments = new List<Assignment>(ids.Count);
		for (var index = 0; index < ids.Count; index++)
		{
			assignments.Add(new Assignment
			{
				EventId = eventId,
				DraftRunId = draftRunId,
				SantaTelegramUserId = long.Parse(ids[index]),
				RecipientTelegramUserId = long.Parse(recipients[index])
			});
		}

		return assignments;
	}
}
