using System.Collections.Concurrent;
using NUnit.Framework;
using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Handlers.Commands.Start;
using SecretSanta.Domain.Models;
using SecretSanta.Domain.Services;

namespace SecretSanta.Domain.Tests;

public class SecretSantaWorkflowTests
{
	[Test]
	public async Task CreateNewEvent_GeneratesUniqueJoinCode_AndRegistersOrganizerAsParticipant()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var repo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var participantService = new ParticipantService(participantRepo);
		var pending = new PendingUserInputService();

		var createHandler = new StartCreateHandler(participantService, repo, pending);
		var result = await createHandler.Handle(new StartCreateRequest
		{
			EventId = "evt-1",
			TelegramUserId = "101",
			DisplayName = "Organizer"
		}, CancellationToken.None);

		Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
		Assert.That(result.Payload, Is.Not.Null);
		var eventEntity = eventStore.Values.Single();
		Assert.That(eventEntity.JoinCode, Is.Not.Empty.And.Not.EqualTo(""));
		Assert.That(eventEntity.JoinCode.Length, Is.EqualTo(8));
		Assert.That(eventEntity.OrganizerTelegramId, Is.EqualTo(101));
		Assert.That(participantStore.Values.Count(p => p.EventId == eventEntity.EventId && p.TelegramUserId == "101"), Is.EqualTo(1));
	}

	[Test]
	public async Task JoiningFiveToSixParticipants_ToExistingEvent_AddsAllUsers()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var repo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var participantService = new ParticipantService(participantRepo);
		var pending = new PendingUserInputService();

		var organizerId = "100";
		var eventEntity = new Event { EventId = "evt-2", Name = "Secret Santa", JoinCode = "ABC12345", OrganizerTelegramId = long.Parse(organizerId), CreatedByTelegramId = long.Parse(organizerId), State = "Registration" };
		await repo.CreateAsync(eventEntity);

		var joinHandler = new StartJoinHandler(participantService, repo, pending);
		var participantIds = new[] { "1001", "1002", "1003", "1004", "1005", "1006" };
		foreach (var participantId in participantIds)
		{
			var result = await joinHandler.Handle(new StartJoinRequest
			{
				EventId = eventEntity.EventId,
				TelegramUserId = participantId,
				JoinCode = eventEntity.JoinCode,
				DisplayName = $"User-{participantId}"
			}, CancellationToken.None);

			Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
		}

		Assert.That(participantStore.Values.Count(p => p.EventId == eventEntity.EventId), Is.EqualTo(participantIds.Length));
		Assert.That(participantStore.Values.Any(p => p.EventId == eventEntity.EventId && p.TelegramUserId == "100"), Is.False);
	}

	[Test]
	public async Task CreatingProfiles_ForFiveToSixParticipants_StoresExpectedProfileData()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var repo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var participantService = new ParticipantService(participantRepo);

		var eventEntity = new Event { EventId = "evt-3", Name = "Secret Santa", JoinCode = "PROFILE1", OrganizerTelegramId = 1001, CreatedByTelegramId = 1001, State = "Registration" };
		await repo.CreateAsync(eventEntity);

		var participantIds = new[] { "2001", "2002", "2003", "2004", "2005", "2006" };
		foreach (var participantId in participantIds)
		{
			var participant = new ParticipantDto
			{
				EventId = eventEntity.EventId,
				TelegramUserId = participantId,
				DisplayName = $"Profile-{participantId}",
				WishlistText = $"Wishlist-{participantId}",
				Hobbies = "books",
				PreferredGiftCategories = "games",
				AvoidCategories = "tech",
				PreferredColors = "blue",
				OtherPreferences = "No perfume"
			};
			await participantService.CreateAsync(participant);
		}

		var saved = participantStore.Values.Where(p => p.EventId == eventEntity.EventId).ToList();
		Assert.That(saved.Count, Is.EqualTo(participantIds.Length));
		Assert.That(saved.All(p => !string.IsNullOrWhiteSpace(p.WishlistText)), Is.True);
		Assert.That(saved.All(p => !string.IsNullOrWhiteSpace(p.Hobbies)), Is.True);
		Assert.That(saved.All(p => !string.IsNullOrWhiteSpace(p.PreferredGiftCategories)), Is.True);
	}

	[Test]
	public async Task Draft_ShouldAssignEveryParticipantExactlyOneRecipient_AndEachSantaExactlyOneRecipient()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var assignmentStore = new ConcurrentDictionary<string, Assignment>();
		var repo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var assignmentRepo = new FakeAssignmentRepository(assignmentStore);
		var draftService = new DraftService(participantRepo, assignmentRepo, repo);
		var eventEntity = new Event { EventId = "evt-4", Name = "Secret Santa", JoinCode = "DRAFT1", OrganizerTelegramId = 301, CreatedByTelegramId = 301, State = "Registration" };
		await repo.CreateAsync(eventEntity);

		var userIds = new[] { "301", "302", "303", "304", "305", "306" };
		foreach (var userId in userIds)
		{
			participantStore[$"{eventEntity.EventId}:{userId}"] = new Participant
			{
				Id = $"{eventEntity.EventId}:{userId}",
				EventId = eventEntity.EventId,
				TelegramUserId = userId,
				DisplayName = $"User-{userId}",
				RegistrationStatus = "Registered"
			};
		}

		await draftService.StartDraftAsync(eventEntity.EventId, eventEntity.OrganizerTelegramId, CancellationToken.None);
		var assignments = assignmentStore.Values.Where(a => a.EventId == eventEntity.EventId).ToList();

		Assert.That(assignments.Count, Is.EqualTo(userIds.Length));
		var santaIds = assignments.Select(a => a.SantaTelegramUserId.ToString()).ToList();
		var recipientIds = assignments.Select(a => a.RecipientTelegramUserId.ToString()).ToList();
		Assert.That(santaIds.Distinct().Count(), Is.EqualTo(userIds.Length));
		Assert.That(recipientIds.Distinct().Count(), Is.EqualTo(userIds.Length));
		Assert.That(santaIds, Is.SupersetOf(userIds));
		Assert.That(recipientIds, Is.SupersetOf(userIds));
		Assert.That(assignments.All(a => a.SantaTelegramUserId != a.RecipientTelegramUserId), Is.True);
		Assert.That(assignments.Select(a => a.SantaTelegramUserId).Distinct().Count(), Is.EqualTo(assignments.Select(a => a.SantaTelegramUserId).Count()));
		Assert.That(assignments.Select(a => a.RecipientTelegramUserId).Distinct().Count(), Is.EqualTo(assignments.Select(a => a.RecipientTelegramUserId).Count()));
	}

	[Test]
	public async Task VoteSession_ShouldReachMajority_AndAggregateCompromiseResult()
	{
		var voteRepo = new FakeVoteRepository();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var eventStore = new ConcurrentDictionary<string, Event>();
		var eventRepo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var voteService = new VoteService(voteRepo, participantRepo, eventRepo);

		var eventEntity = new Event { EventId = "evt-5", Name = "Secret Santa", JoinCode = "VOTE1", OrganizerTelegramId = 401, CreatedByTelegramId = 401, State = "Voting" };
		await eventRepo.CreateAsync(eventEntity);
		var session = new VoteSession
		{
			Id = "session-1",
			EventId = eventEntity.EventId,
			Options = new List<VoteOption>
			{
				new() { Id = "budget-10", Label = "10", Value = 10 },
				new() { Id = "budget-20", Label = "20", Value = 20 },
				new() { Id = "budget-30", Label = "30", Value = 30 }
			}
		};
		await voteService.CreateVoteSessionAsync(eventEntity.EventId, session, CancellationToken.None);

		await voteService.SubmitVoteAsync(eventEntity.EventId, session.Id, new Vote { EventId = eventEntity.EventId, VoteSessionId = session.Id, VoterTelegramId = 401, OptionId = "budget-10" }, CancellationToken.None);
		await voteService.SubmitVoteAsync(eventEntity.EventId, session.Id, new Vote { EventId = eventEntity.EventId, VoteSessionId = session.Id, VoterTelegramId = 402, OptionId = "budget-10" }, CancellationToken.None);
		await voteService.SubmitVoteAsync(eventEntity.EventId, session.Id, new Vote { EventId = eventEntity.EventId, VoteSessionId = session.Id, VoterTelegramId = 403, OptionId = "budget-20" }, CancellationToken.None);
		await voteService.SubmitVoteAsync(eventEntity.EventId, session.Id, new Vote { EventId = eventEntity.EventId, VoteSessionId = session.Id, VoterTelegramId = 404, OptionId = "budget-20" }, CancellationToken.None);
		await voteService.SubmitVoteAsync(eventEntity.EventId, session.Id, new Vote { EventId = eventEntity.EventId, VoteSessionId = session.Id, VoterTelegramId = 405, OptionId = "budget-20" }, CancellationToken.None);

		var result = await voteService.AggregateResultAsync(eventEntity.EventId, session.Id, CancellationToken.None);
		Assert.That(result, Is.EqualTo("budget-20"));
	}

	private sealed class FakeEventRepository(ConcurrentDictionary<string, Event> store) : IEventRepository
	{
		public Task CreateAsync(Event ev, CancellationToken cancellationToken = default)
		{
			store[ev.EventId] = ev;
			return Task.CompletedTask;
		}

		public Task<Event?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken = default)
		{
			store.TryGetValue(eventId, out var ev);
			return Task.FromResult(ev);
		}

		public Task<Event?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
		{
			var ev = store.Values.FirstOrDefault(x => x.JoinCode == joinCode);
			return Task.FromResult(ev);
		}

		public Task UpdateAsync(Event ev, CancellationToken cancellationToken = default)
		{
			store[ev.EventId] = ev;
			return Task.CompletedTask;
		}

		public Task<IEnumerable<Event>> QueryByStateAsync(string state, CancellationToken cancellationToken = default)
		{
			return Task.FromResult<IEnumerable<Event>>(store.Values.Where(x => x.State == state).ToList());
		}
	}

	private sealed class FakeParticipantRepository(ConcurrentDictionary<string, Participant> store) : IParticipantRepository
	{
		public Task<Participant?> GetByTelegramUserIdAsync(string eventId, string telegramUserId, CancellationToken cancellationToken = default)
		{
			var participant = store.Values.FirstOrDefault(x => x.EventId == eventId && x.TelegramUserId == telegramUserId);
			return Task.FromResult(participant);
		}

		public Task CreateAsync(Participant participant, CancellationToken cancellationToken = default)
		{
			store[$"{participant.EventId}:{participant.TelegramUserId}"] = participant;
			return Task.CompletedTask;
		}

		public Task UpdateAsync(Participant participant, CancellationToken cancellationToken = default)
		{
			store[$"{participant.EventId}:{participant.TelegramUserId}"] = participant;
			return Task.CompletedTask;
		}

		public Task<IEnumerable<Participant>> GetAllByEventAsync(string eventId, CancellationToken cancellationToken = default)
		{
			return Task.FromResult<IEnumerable<Participant>>(store.Values.Where(x => x.EventId == eventId).ToList());
		}
	}

	private sealed class FakeAssignmentRepository(ConcurrentDictionary<string, Assignment> store) : IAssignmentRepository
	{
		public Task CreateAssignmentsAsync(IEnumerable<Assignment> assignments, CancellationToken cancellationToken = default)
		{
			foreach (var assignment in assignments)
			{
				store[$"{assignment.EventId}:{assignment.SantaTelegramUserId}:{assignment.RecipientTelegramUserId}"] = assignment;
			}
			return Task.CompletedTask;
		}

		public Task<IEnumerable<Assignment>> GetAssignmentsByEventAsync(string eventId, CancellationToken cancellationToken = default)
		{
			return Task.FromResult<IEnumerable<Assignment>>(store.Values.Where(x => x.EventId == eventId).ToList());
		}

		public Task<Assignment?> GetAssignmentForRecipientAsync(string eventId, long recipientTelegramId, CancellationToken cancellationToken = default)
		{
			var assignment = store.Values.FirstOrDefault(x => x.EventId == eventId && x.RecipientTelegramUserId == recipientTelegramId);
			return Task.FromResult(assignment);
		}

		public Task<Assignment?> GetAssignmentForSantaAsync(string eventId, long santaTelegramId, CancellationToken cancellationToken = default)
		{
			var assignment = store.Values.FirstOrDefault(x => x.EventId == eventId && x.SantaTelegramUserId == santaTelegramId);
			return Task.FromResult(assignment);
		}
	}

	private sealed class FakeVoteRepository : IVoteRepository
	{
		private readonly List<Vote> _votes = new();
		private readonly List<VoteSession> _sessions = new();

		public Task CreateVoteSessionAsync(VoteSession session, CancellationToken cancellationToken = default)
		{
			_sessions.Add(session);
			return Task.CompletedTask;
		}

		public Task<VoteSession?> GetVoteSessionAsync(string eventId, string sessionId, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(_sessions.FirstOrDefault(s => s.EventId == eventId && s.Id == sessionId));
		}

		public Task CreateVoteAsync(Vote vote, CancellationToken cancellationToken = default)
		{
			_votes.Add(vote);
			return Task.CompletedTask;
		}

		public Task<IEnumerable<Vote>> GetVotesForSessionAsync(string eventId, string sessionId, CancellationToken cancellationToken = default)
		{
			return Task.FromResult<IEnumerable<Vote>>(_votes.Where(v => v.EventId == eventId && v.VoteSessionId == sessionId).ToList());
		}

		public Task CloseVoteSessionAsync(VoteSession session, CancellationToken cancellationToken = default)
		{
			session.Status = "Closed";
			return Task.CompletedTask;
		}
	}
}
