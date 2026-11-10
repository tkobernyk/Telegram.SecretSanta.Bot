using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using NSubstitute;
using MediatR;
using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Enums;
using SecretSanta.Domain.Handlers.Commands.Conversation;
using SecretSanta.Domain.Handlers.Commands.Participant;
using SecretSanta.Domain.Handlers.Commands.Start;
using SecretSanta.Domain.Handlers.Commands.Voting;
using SecretSanta.Domain.Handlers.TelegramEndpoint;
using SecretSanta.Domain.Models;
using SecretSanta.Domain.Services;
using SecretSanta.Infrastructure.AI;
using SecretSanta.Infrastructure.Telegram;
using Telegram.Bot.Types.ReplyMarkups;

namespace SecretSanta.Domain.Tests;

public class SecretSantaWorkflowTests
{
	[Test]
	public async Task FullWorkflow_CreateEvent_JoinParticipants_Vote_Revote_Draft_Conversation_Cancel_AndFinalizeGiftDelivery()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var assignmentStore = new ConcurrentDictionary<string, Assignment>();
		var voteStore = new ConcurrentDictionary<string, Vote>();
		var eventRepo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var assignmentRepo = new FakeAssignmentRepository(assignmentStore);
		var voteRepo = new FakeVoteRepository(voteStore);
		var participantService = new ParticipantService(participantRepo);
		var pending = new PendingUserInputService();
		var telegram = new FakeTelegramSender();
		var textGeneration = new FakeTextGenerationService();
		var conversationRepo = new FakeConversationRepository();
		var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Conversation:TimeoutMinutes"] = "60" }).Build();
		var voteService = new VoteService(voteRepo, participantRepo, eventRepo);
		var orchestrator = new ConversationOrchestrator(textGeneration, pending, telegram, participantService, new FakeWizardRegistry(), conversationRepo, config, NullLogger<ConversationOrchestrator>.Instance);
		var startCreateHandler = new StartCreateHandler(participantService, eventRepo, pending);
		var organizerId = "100";
		var organizerCreate = await startCreateHandler.Handle(new StartCreateRequest { EventId = "evt-full", TelegramUserId = organizerId, DisplayName = "Organizer" }, CancellationToken.None);
		Assert.That(organizerCreate.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));

		var eventEntity = eventStore.Values.Single();
		Assert.That(eventEntity.State, Is.EqualTo(EventWorkflowState.Registration));

		var joinHandler = new StartJoinHandler(participantService, eventRepo, pending);
		var joiners = new[] { "101", "102", "103", "104", "105", "106" };
		foreach (var joinerId in joiners)
		{
			var result = await joinHandler.Handle(new StartJoinRequest
			{
				EventId = eventEntity.EventId,
				TelegramUserId = joinerId,
				JoinCode = eventEntity.JoinCode,
				DisplayName = $"User-{joinerId}"
			}, CancellationToken.None);
			Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));

			var profile = await participantService.GetByTelegramUserIdAsync(eventEntity.EventId, joinerId, CancellationToken.None);
			Assert.That(profile, Is.Not.Null);
			profile!.WishlistText = $"Книжка для {joinerId}";
			profile.Hobbies = "Книги";
			profile.PreferredGiftCategories = "Книги";
			await participantService.UpdateAsync(profile, CancellationToken.None);
		}

		Assert.That(participantStore.Values.Count(p => p.EventId == eventEntity.EventId), Is.EqualTo(7));

		var organizerVoteRequest = new StartVoteSessionRequest
		{
			EventId = eventEntity.EventId,
			InitiatorTelegramId = 100,
			Session = new VoteSession { EventId = eventEntity.EventId }
		};
		var startVoteHandler = new StartVoteSessionHandler(voteService, eventRepo, participantRepo, telegram);
		var startVoteResponse = await startVoteHandler.Handle(organizerVoteRequest, CancellationToken.None);
		Assert.That(startVoteResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
		Assert.That(eventStore[eventEntity.EventId].State, Is.EqualTo(EventWorkflowState.Voting));

		var submitVoteHandler = new SubmitBudgetVoteHandler(eventRepo, participantRepo, voteService, assignmentRepo, new DraftService(participantRepo, assignmentRepo, eventRepo), telegram);
		var firstRoundVotes = new Dictionary<long, long>
		{
			[100] = 500,
			[101] = 500,
			[102] = 500,
			[103] = 750,
			[104] = 750,
			[105] = 500,
			[106] = 500,
		};
		foreach (var vote in firstRoundVotes)
		{
			var res = await submitVoteHandler.Handle(new SubmitBudgetVoteRequest { EventId = eventEntity.EventId, TelegramUserId = vote.Key, Budget = vote.Value }, CancellationToken.None);
			Assert.That(res.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK).Or.EqualTo(System.Net.HttpStatusCode.NoContent));
		}
		Assert.That(eventStore[eventEntity.EventId].State, Is.EqualTo(EventWorkflowState.BudgetDiscussion));

		var secondSession = new VoteSession { Id = "vote-round-2", EventId = eventEntity.EventId, Options = new List<VoteOption> { new() { Id = "250", Label = "250 грн", Value = 250 }, new() { Id = "500", Label = "500 грн", Value = 500 }, new() { Id = "750", Label = "750 грн", Value = 750 } } };
		await voteService.CreateVoteSessionAsync(eventEntity.EventId, secondSession, CancellationToken.None);
		foreach (var userId in new[] { 100L, 101L, 102L, 103L, 104L, 105L, 106L })
		{
			var res = await submitVoteHandler.Handle(new SubmitBudgetVoteRequest { EventId = eventEntity.EventId, TelegramUserId = userId, Budget = 500 }, CancellationToken.None);
			Assert.That(res.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK).Or.EqualTo(System.Net.HttpStatusCode.NoContent));
		}
		Assert.That(eventStore[eventEntity.EventId].State, Is.EqualTo(EventWorkflowState.Draft));

		var assignments = assignmentStore.Values.Where(a => a.EventId == eventEntity.EventId).ToList();
		Assert.That(assignments.Count, Is.EqualTo(7));
		var firstAssignment = assignments.First();
		var santaId = firstAssignment.SantaTelegramUserId.ToString();
		var recipientId = firstAssignment.RecipientTelegramUserId.ToString();
		await orchestrator.StartConversationAsync(eventEntity.EventId, santaId, recipientId, santaId, CancellationToken.None);
		Assert.That(conversationRepo.Created.Count, Is.EqualTo(1));
		Assert.That(textGeneration.LastPrompt, Does.Contain("Вішліст отримувача"));
		Assert.That(textGeneration.LastPrompt, Does.Contain("українською"));

		var cancelHandler = new CancelConversationHandler(orchestrator);
		await cancelHandler.Handle(new CancelConversationRequest { EventId = eventEntity.EventId, InitiatorTelegramUserId = recipientId }, CancellationToken.None);
		Assert.That(telegram.Messages.Any(m => m.UserId == recipientId && m.Text.Contains("Діалог завершено.")), Is.True);
		Assert.That(
			telegram.SentMessages.Where(m => m.Text.Contains("Діалог завершено."))
				.All(m => HasPostConversationActions(m.Markup)),
			Is.True);
		Assert.That(telegram.Messages.Count(m => m.Text.Contains("Діалог завершено.")), Is.EqualTo(2));

		var readyHandler = new MarkReadyHandler(participantRepo, eventRepo, pending, telegram);
		var participants = (await participantRepo.GetAllByEventAsync(eventEntity.EventId, CancellationToken.None)).ToList();
		for (var index = 0; index < participants.Count; index++)
		{
			var participant = participants[index];
			var result = await readyHandler.Handle(new MarkReadyRequest { EventId = eventEntity.EventId, TelegramUserId = long.Parse(participant.TelegramUserId), GiftStatus = GiftStatusValue.Ready }, CancellationToken.None);
			Assert.That(result.StatusCode, Is.EqualTo(index == participants.Count - 1
				? System.Net.HttpStatusCode.NoContent
				: System.Net.HttpStatusCode.OK));
		}
		Assert.That(eventStore[eventEntity.EventId].State, Is.EqualTo(EventWorkflowState.AwaitingDelivery));
		Assert.That(pending.TryGetConversation(eventEntity.OrganizerTelegramId.ToString(), out var state), Is.True);
		Assert.That(state, Is.Not.Null);
		var deliveryState = state!;
		Assert.That(deliveryState.WizardName, Is.EqualTo("delivery_info"));

		var deliveryText = "Київ, вул. Хрещатик 1, 18:00";
		var persistedEvent = await eventRepo.GetByEventIdAsync(eventEntity.EventId, CancellationToken.None);
		Assert.That(persistedEvent, Is.Not.Null);
		eventEntity = persistedEvent!;
		eventEntity.DeliveryLocation = deliveryText;
		eventEntity.State = EventWorkflowState.Done;
		eventEntity.UpdatedAtUtc = DateTime.UtcNow;
		await eventRepo.UpdateAsync(eventEntity, CancellationToken.None);
		foreach (var participant in await participantRepo.GetAllByEventAsync(eventEntity.EventId, CancellationToken.None))
		{
			await telegram.SendMessageAsync(participant.TelegramUserId, $"Місце й час вручення Secret Santa: {deliveryText}");
		}
		Assert.That(telegram.Messages.Any(m => m.UserId == "101" && m.Text.Contains(deliveryText)), Is.True);
		Assert.That(telegram.Messages.Any(m => m.UserId == "106" && m.Text.Contains(deliveryText)), Is.True);
		Assert.That(eventStore[eventEntity.EventId].State, Is.EqualTo(EventWorkflowState.Done));
	}

	[Test]
	public async Task MultipleEvents_AreLogicallySeparated_ByEventId()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var repo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var participantService = new ParticipantService(participantRepo);
		var pending = new PendingUserInputService();
		var createHandler = new StartCreateHandler(participantService, repo, pending);

		var first = await createHandler.Handle(new StartCreateRequest { EventId = "evt-one", TelegramUserId = "100", DisplayName = "OrganizerOne" }, CancellationToken.None);
		var second = await createHandler.Handle(new StartCreateRequest { EventId = "evt-two", TelegramUserId = "200", DisplayName = "OrganizerTwo" }, CancellationToken.None);

		Assert.That(first.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
		Assert.That(second.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));

		var event1 = eventStore.Values.Single(e => e.OrganizerTelegramId == 100);
		var event2 = eventStore.Values.Single(e => e.OrganizerTelegramId == 200);
		Assert.That(event1.EventId, Is.Not.EqualTo(event2.EventId));

		var sameUserInEvent1 = await participantService.GetByTelegramUserIdAsync(event1.EventId, "100", CancellationToken.None);
		var sameUserInEvent2 = await participantService.GetByTelegramUserIdAsync(event2.EventId, "200", CancellationToken.None);
		Assert.That(sameUserInEvent1, Is.Not.Null);
		Assert.That(sameUserInEvent2, Is.Not.Null);

		var event1Participants = (await participantRepo.GetAllByEventAsync(event1.EventId, CancellationToken.None)).ToList();
		var event2Participants = (await participantRepo.GetAllByEventAsync(event2.EventId, CancellationToken.None)).ToList();
		Assert.That(event1Participants.Select(p => p.TelegramUserId), Does.Contain("100").And.Not.Contain("200"));
		Assert.That(event2Participants.Select(p => p.TelegramUserId), Does.Contain("200").And.Not.Contain("100"));
		Assert.That(event1Participants.Select(p => p.EventId).Distinct(), Is.EquivalentTo(new[] { event1.EventId }));
		Assert.That(event2Participants.Select(p => p.EventId).Distinct(), Is.EquivalentTo(new[] { event2.EventId }));
	}

	[Test]
	public async Task StartCreateHandler_SetsActiveEvent_AndRetainsItAfterConversationEnds()
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
		Assert.That(pending.TryGetActiveEventId("101", out var activeEventId), Is.True);
		Assert.That(string.IsNullOrWhiteSpace(activeEventId), Is.False);

		pending.EndConversation("101");
		Assert.That(pending.TryGetActiveEventId("101", out var retainedEventId), Is.True);
		Assert.That(retainedEventId, Is.EqualTo(activeEventId));
	}

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
	public async Task StartHandler_WhenUserRequests_Start_ReturnsChoiceButtons()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var repo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var participantService = new ParticipantService(participantRepo);
		var pending = new PendingUserInputService();
		var handler = new StartHandler(participantService, repo, pending);

		var response = await handler.Handle(new StartRequest
		{
			EventId = "evt-start",
			Message = new TelegramMessage { Text = "/start", From = new TelegramUser { Id = 777, FirstName = "Test", Username = "testuser" } }
		}, CancellationToken.None);

		Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
		Assert.That(response.ResponseMessage, Is.EqualTo("Виберіть дію:"));
		Assert.That(response.ReplyMarkup, Is.Not.Null);
		Assert.That(participantStore, Is.Empty);
		Assert.That(pending.TryGetConversation("777", out var state), Is.True);
		Assert.That(state, Is.Not.Null);
		Assert.That(state!.WizardName, Is.EqualTo("start_event"));
	}

	[Test]
	public async Task StartCreateHandler_AutoRegistersOrganizer_WhenNewEventIsCreated()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var repo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var participantService = new ParticipantService(participantRepo);
		var pending = new PendingUserInputService();
		var createHandler = new StartCreateHandler(participantService, repo, pending);

		var result = await createHandler.Handle(new StartCreateRequest { EventId = "evt-auto", TelegramUserId = "202", DisplayName = "AutoReg" }, CancellationToken.None);

		Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
		Assert.That(result.ResponseMessage, Does.Not.Contain("/profile").And.Not.Contain("/wishlist"));
		Assert.That(result.ReplyMarkup, Is.TypeOf<InlineKeyboardMarkup>());
		Assert.That(participantStore.Values.Any(p => p.EventId == eventStore.Values.Single().EventId && p.TelegramUserId == "202"), Is.True);
		Assert.That(pending.TryGetActiveEventId("202", out var activeEventId), Is.True);
		Assert.That(activeEventId, Is.EqualTo(eventStore.Values.Single().EventId));
	}

	[Test]
	public async Task StartHandler_CreateButton_AutoRegistersOrganizerAndEndsStartWizard()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var repo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var participantService = new ParticipantService(participantRepo);
		var pending = new PendingUserInputService();
		var handler = new StartHandler(participantService, repo, pending);
		var from = new TelegramUser { Id = 202, FirstName = "AutoReg", Username = "autoreg" };
		pending.StartConversation("202", new ConversationState { WizardName = "start_event", StepIndex = 0 });

		var response = await handler.Handle(new StartRequest
		{
			EventId = "evt-default",
			Message = new TelegramMessage { CallbackData = "start:create", From = from }
		}, CancellationToken.None);

		var eventEntity = eventStore.Values.Single();
		Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
		Assert.That(participantStore.Values.Count(p => p.EventId == eventEntity.EventId && p.TelegramUserId == "202"), Is.EqualTo(1));
		Assert.That(pending.TryGetConversation("202", out _), Is.False);
		Assert.That(pending.TryGetActiveEventId("202", out var activeEventId), Is.True);
		Assert.That(activeEventId, Is.EqualTo(eventEntity.EventId));
	}

	[Test]
	public async Task StartJoinHandler_AutoRegistersUser_WhenJoinCodeIsValid()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var repo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var participantService = new ParticipantService(participantRepo);
		var pending = new PendingUserInputService();
		var eventEntity = new Event { EventId = "evt-join", Name = "Join Event", JoinCode = "JOIN1234", OrganizerTelegramId = 999, CreatedByTelegramId = 999, State = EventWorkflowState.Registration };
		await repo.CreateAsync(eventEntity);
		var joinHandler = new StartJoinHandler(participantService, repo, pending);

		var result = await joinHandler.Handle(new StartJoinRequest { EventId = eventEntity.EventId, TelegramUserId = "303", JoinCode = eventEntity.JoinCode, DisplayName = "New Joiner" }, CancellationToken.None);

		Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
		Assert.That(result.ResponseMessage, Does.Not.Contain("/profile").And.Not.Contain("/wishlist"));
		Assert.That(result.ReplyMarkup, Is.TypeOf<InlineKeyboardMarkup>());
		Assert.That(participantStore.Values.Any(p => p.EventId == eventEntity.EventId && p.TelegramUserId == "303"), Is.True);
		Assert.That(pending.TryGetActiveEventId("303", out var activeEventId), Is.True);
		Assert.That(activeEventId, Is.EqualTo(eventEntity.EventId));
	}

	[Test]
	public async Task StartHandler_JoinButtonThenJoinCode_AutoRegistersUser()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var repo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var participantService = new ParticipantService(participantRepo);
		var pending = new PendingUserInputService();
		var handler = new StartHandler(participantService, repo, pending);
		var eventEntity = new Event { EventId = "evt-join-flow", Name = "Join Event", JoinCode = "JOIN4321", OrganizerTelegramId = 999, CreatedByTelegramId = 999, State = EventWorkflowState.Registration };
		await repo.CreateAsync(eventEntity);
		var from = new TelegramUser { Id = 303, FirstName = "Joiner", Username = "joiner" };
		pending.StartConversation("303", new ConversationState { WizardName = "start_event", StepIndex = 0 });

		var prompt = await handler.Handle(new StartRequest
		{
			EventId = "default-event",
			Message = new TelegramMessage { CallbackData = "start:join", From = from }
		}, CancellationToken.None);

		Assert.That(prompt.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
		Assert.That(pending.TryGetConversation("303", out var joinState), Is.True);
		Assert.That(joinState?.StepIndex, Is.EqualTo(1));

		var joined = await handler.Handle(new StartRequest
		{
			EventId = "default-event",
			Message = new TelegramMessage { Text = eventEntity.JoinCode, From = from }
		}, CancellationToken.None);

		Assert.That(joined.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
		Assert.That(participantStore.Values.Count(p => p.EventId == eventEntity.EventId && p.TelegramUserId == "303"), Is.EqualTo(1));
		Assert.That(pending.TryGetConversation("303", out _), Is.False);
		Assert.That(pending.TryGetActiveEventId("303", out var activeEventId), Is.True);
		Assert.That(activeEventId, Is.EqualTo(eventEntity.EventId));
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
		var voteStore = new ConcurrentDictionary<string, Vote>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var eventStore = new ConcurrentDictionary<string, Event>();
		var eventRepo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var voteRepo = new FakeVoteRepository(voteStore);
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

	[Test]
	public async Task StartVoteSession_WithTwoParticipants_AllowsOrganizerToStartFromBudgetConfirmation()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var eventRepo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var voteRepo = new FakeVoteRepository();
		var voteService = new VoteService(voteRepo, participantRepo, eventRepo);
		var telegram = new FakeTelegramSender();
		var eventEntity = new Event
		{
			EventId = "evt-start-vote-2",
			Name = "Secret Santa",
			JoinCode = "VOTE2",
			OrganizerTelegramId = 900,
			CreatedByTelegramId = 900,
			State = EventWorkflowState.BudgetConfirmation
		};
		await eventRepo.CreateAsync(eventEntity);
		foreach (var userId in new[] { "900", "901" })
		{
			participantStore[$"{eventEntity.EventId}:{userId}"] = new Participant
			{
				EventId = eventEntity.EventId,
				TelegramUserId = userId,
				DisplayName = $"User-{userId}",
				RegistrationStatus = "Registered"
			};
		}
		var handler = new StartVoteSessionHandler(voteService, eventRepo, participantRepo, telegram);

		var response = await handler.Handle(new StartVoteSessionRequest
		{
			EventId = eventEntity.EventId,
			InitiatorTelegramId = eventEntity.OrganizerTelegramId,
			Session = new VoteSession { EventId = eventEntity.EventId }
		}, CancellationToken.None);

		Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
		Assert.That(eventStore[eventEntity.EventId].State, Is.EqualTo(EventWorkflowState.Voting));
		Assert.That(string.IsNullOrWhiteSpace(eventStore[eventEntity.EventId].CurrentVoteSessionId), Is.False);
		Assert.That(telegram.Messages.Count, Is.EqualTo(2));
	}

	[Test]
	public async Task StartVoteSession_WithFewerThanTwoParticipants_IsRejected()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var eventRepo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var voteRepo = new FakeVoteRepository();
		var voteService = new VoteService(voteRepo, participantRepo, eventRepo);
		var eventEntity = new Event { EventId = "evt-start-vote-1", OrganizerTelegramId = 910, State = EventWorkflowState.Registration };
		await eventRepo.CreateAsync(eventEntity);
		participantStore[$"{eventEntity.EventId}:910"] = new Participant { EventId = eventEntity.EventId, TelegramUserId = "910", DisplayName = "Organizer" };
		var handler = new StartVoteSessionHandler(voteService, eventRepo, participantRepo, new FakeTelegramSender());

		var response = await handler.Handle(new StartVoteSessionRequest
		{
			EventId = eventEntity.EventId,
			InitiatorTelegramId = eventEntity.OrganizerTelegramId,
			Session = new VoteSession { EventId = eventEntity.EventId }
		}, CancellationToken.None);

		Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
		Assert.That(response.ResponseMessage, Does.Contain("щонайменше два учасники"));
		Assert.That(eventStore[eventEntity.EventId].State, Is.EqualTo(EventWorkflowState.Registration));
	}

	[Test]
	public async Task SubmitBudgetVote_WhenConsensusReached_StartsDraft_AndSendsAssignmentMessages()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var assignmentStore = new ConcurrentDictionary<string, Assignment>();
		var voteStore = new ConcurrentDictionary<string, Vote>();
		var eventRepo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var assignmentRepo = new FakeAssignmentRepository(assignmentStore);
		var voteRepo = new FakeVoteRepository(voteStore);
		var voteService = new VoteService(voteRepo, participantRepo, eventRepo);
		var draftService = new DraftService(participantRepo, assignmentRepo, eventRepo);
		var telegram = new FakeTelegramSender();
		var handler = new SubmitBudgetVoteHandler(eventRepo, participantRepo, voteService, assignmentRepo, draftService, telegram);

		var eventEntity = new Event { EventId = "evt-6", Name = "Secret Santa", JoinCode = "BUDGET1", OrganizerTelegramId = 500, CreatedByTelegramId = 500, State = "Registration" };
		await eventRepo.CreateAsync(eventEntity);

		foreach (var userId in new[] { "500", "501", "502" })
		{
			participantStore[$"{eventEntity.EventId}:{userId}"] = new Participant
			{
				Id = $"{eventEntity.EventId}:{userId}",
				EventId = eventEntity.EventId,
				TelegramUserId = userId,
				DisplayName = $"User-{userId}",
				WishlistText = $"Wishlist-{userId}",
				RegistrationStatus = "Registered"
			};
		}

		var session = new VoteSession
		{
			Id = "session-budget",
			EventId = eventEntity.EventId,
			Options = new List<VoteOption>
			{
				new() { Id = "250", Value = 250 },
				new() { Id = "500", Value = 500 },
				new() { Id = "750", Value = 750 },
				new() { Id = "1000", Value = 1000 },
				new() { Id = "1500", Value = 1500 }
			}
		};
		await voteService.CreateVoteSessionAsync(eventEntity.EventId, session, CancellationToken.None);

		var first = await handler.Handle(new SubmitBudgetVoteRequest { EventId = eventEntity.EventId, TelegramUserId = 500, Budget = 500 }, CancellationToken.None);
		Assert.That(first.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
		var second = await handler.Handle(new SubmitBudgetVoteRequest { EventId = eventEntity.EventId, TelegramUserId = 501, Budget = 500 }, CancellationToken.None);
		Assert.That(second.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
		var third = await handler.Handle(new SubmitBudgetVoteRequest { EventId = eventEntity.EventId, TelegramUserId = 502, Budget = 500 }, CancellationToken.None);
		Assert.That(third.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NoContent));
		Assert.That(third.ResponseMessage, Is.Null);

		Assert.That(eventStore[eventEntity.EventId].State, Is.EqualTo("Draft"));
		Assert.That(assignmentStore.Values.Count(a => a.EventId == eventEntity.EventId), Is.EqualTo(3));
		Assert.That(telegram.Messages.Any(m => m.Text.Contains("Ваш отримувач") && m.Text.Contains("Вішліст:")), Is.True);
		Assert.That(telegram.SentMessages.Count, Is.EqualTo(3));
		Assert.That(telegram.SentMessages.Select(m => m.UserId).Distinct().Count(), Is.EqualTo(3));

		var replay = await handler.Handle(new SubmitBudgetVoteRequest { EventId = eventEntity.EventId, TelegramUserId = 502, Budget = 500 }, CancellationToken.None);
		Assert.That(replay.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NoContent));
		Assert.That(replay.ResponseMessage, Is.Null);
		Assert.That(telegram.SentMessages.Count, Is.EqualTo(3));
	}

	[TestCase(EventWorkflowState.Registration, new[] { "profile:view", "wishlist:edit" })]
	[TestCase(EventWorkflowState.Draft, new[] { "chat:recipient", "chat:santa", "gift:ready" })]
	public async Task JoiningExistingEvent_ShowsOnlyButtonsAllowedForCurrentState(string state, string[] expectedCallbacks)
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var eventRepo = new FakeEventRepository(eventStore);
		var participantService = new ParticipantService(new FakeParticipantRepository(participantStore));
		var pending = new PendingUserInputService();
		var ev = new Event { EventId = "evt-existing", JoinCode = "EXIST123", OrganizerTelegramId = 900, State = state };
		await eventRepo.CreateAsync(ev);
		participantStore[$"{ev.EventId}:901"] = new Participant { EventId = ev.EventId, TelegramUserId = "901", DisplayName = "Existing" };
		var handler = new StartJoinHandler(participantService, eventRepo, pending);

		var response = await handler.Handle(new StartJoinRequest { EventId = "ignored", TelegramUserId = "901", JoinCode = ev.JoinCode }, CancellationToken.None);

		Assert.That(response.ResponseMessage, Is.EqualTo("Ви вже приєднані до цього Secret Santa."));
		var keyboard = response.ReplyMarkup as Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup;
		Assert.That(keyboard, Is.Not.Null);
		Assert.That(keyboard!.InlineKeyboard.SelectMany(row => row).Select(button => button.CallbackData), Is.EqualTo(expectedCallbacks));
		Assert.That(pending.TryGetConversation("901", out _), Is.False);
	}

	[Test]
	public async Task StartWizardMessage_IsNotAlsoHandledAsAnonymousChatMessage()
	{
		var pending = new PendingUserInputService();
		pending.StartConversation("901", new ConversationState { WizardName = "start_event", StepIndex = 1 });
		var mediator = Substitute.For<IMediator>();
		mediator.Send(Arg.Any<StartRequest>(), Arg.Any<CancellationToken>())
			.Returns(new StartResponse { ResponseMessage = "Ви вже приєднані до цього Secret Santa." });
		var telegram = new FakeTelegramSender();
		var eventRepo = new FakeEventRepository(new ConcurrentDictionary<string, Event>());
		var participantRepo = new FakeParticipantRepository(new ConcurrentDictionary<string, Participant>());
		var handler = new TelegramEndpointHandler(mediator, telegram, pending, eventRepo, participantRepo);

		await handler.Handle(new TelegramEndpointRequest
		{
			EventId = "evt-join",
			Message = new TelegramMessage { Text = "709641C6", From = new TelegramUser { Id = 901 } }
		}, CancellationToken.None);

		await mediator.Received(1).Send(Arg.Any<StartRequest>(), Arg.Any<CancellationToken>());
		Assert.That(mediator.ReceivedCalls().Any(call => call.GetArguments().Any(argument => argument is IncomingMessageRequest)), Is.False);
		Assert.That(telegram.Messages.Count, Is.EqualTo(1));
		Assert.That(telegram.Messages[0].Text, Is.EqualTo("Ви вже приєднані до цього Secret Santa."));
	}

	[Test]
	public async Task MarkReady_WhenAllParticipantsReady_RequestsDeliveryInfoFromOrganizer()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var eventRepo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var pending = new PendingUserInputService();
		var telegram = new FakeTelegramSender();
		var handler = new MarkReadyHandler(participantRepo, eventRepo, pending, telegram);

		var eventEntity = new Event { EventId = "evt-7", Name = "Secret Santa", JoinCode = "READY1", OrganizerTelegramId = 700, CreatedByTelegramId = 700, State = "Preparing" };
		await eventRepo.CreateAsync(eventEntity);
		foreach (var userId in new[] { "700", "701", "702" })
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

		foreach (var userId in new[] { "700", "701", "702" })
		{
			var result = await handler.Handle(new MarkReadyRequest { EventId = eventEntity.EventId, TelegramUserId = long.Parse(userId), GiftStatus = "Ready" }, CancellationToken.None);
			if (userId == "702")
			{
				Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NoContent));
				Assert.That(result.ResponseMessage, Is.Null);
				Assert.That(result.ReplyMarkup, Is.Null);
			}
			else
			{
				Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
				Assert.That(result.ReplyMarkup, Is.Not.Null);
				Assert.That(result.ResponseMessage, Does.Contain("позначений як готовий"));
				var keyboard = (InlineKeyboardMarkup)result.ReplyMarkup!;
				Assert.That(keyboard.InlineKeyboard.SelectMany(row => row).Select(button => button.CallbackData), Is.EqualTo(new[] { "gift:progress" }));
			}
		}

		Assert.That(eventStore[eventEntity.EventId].State, Is.EqualTo("AwaitingDelivery"));
		Assert.That(pending.TryGetConversation("700", out var state), Is.True);
		Assert.That(state!.WizardName, Is.EqualTo("delivery_info"));
		Assert.That(telegram.Messages.Any(m => m.UserId == "700" && m.Text.Contains("місце й час вручення")), Is.True);
		foreach (var userId in new[] { "700", "701", "702" })
		{
			var broadcast = telegram.SentMessages.Single(m => m.UserId == userId && m.Text.Contains("Усі учасники позначили подарунки готовими"));
			Assert.That(broadcast.Markup, Is.Null);
		}
	}

	[Test]
	public async Task ConversationOrchestrator_StartConversation_UsesWishlist_AndUkrainianPrompt()
	{
		var eventStore = new ConcurrentDictionary<string, Event>();
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var repo = new FakeEventRepository(eventStore);
		var participantRepo = new FakeParticipantRepository(participantStore);
		var participantService = new ParticipantService(participantRepo);
		var pending = new PendingUserInputService();
		var telegram = new FakeTelegramSender();
		var textGeneration = new FakeTextGenerationService();
		var conversationRepo = new FakeConversationRepository();
		var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Conversation:TimeoutMinutes"] = "60" }).Build();
		var orchestrator = new ConversationOrchestrator(textGeneration, pending, telegram, participantService, new FakeWizardRegistry(), conversationRepo, config, NullLogger<ConversationOrchestrator>.Instance);

		var eventEntity = new Event { EventId = "evt-8", Name = "Secret Santa", JoinCode = "AI1", OrganizerTelegramId = 800, CreatedByTelegramId = 800, State = "Draft" };
		await repo.CreateAsync(eventEntity);
		participantStore[$"{eventEntity.EventId}:800"] = new Participant { EventId = eventEntity.EventId, TelegramUserId = "800", DisplayName = "Santa", WishlistText = "Книга про космос" };
		participantStore[$"{eventEntity.EventId}:801"] = new Participant { EventId = eventEntity.EventId, TelegramUserId = "801", DisplayName = "Recipient", WishlistText = "Футболка з котом" };

		await orchestrator.StartConversationAsync(eventEntity.EventId, "800", "801", "800", CancellationToken.None);

		Assert.That(textGeneration.LastPrompt, Does.Contain("Вішліст отримувача"));
		Assert.That(textGeneration.LastPrompt, Does.Contain("українською"));
		Assert.That(telegram.Messages.Any(m => m.UserId == "801" && m.Text.Contains("Книга про космос") == false), Is.True);
		Assert.That(telegram.SentMessages.First().Text, Does.Contain("Пробую зв’язатися з AI-помічником Санти"));
		Assert.That(telegram.SentMessages.First().Markup, Is.Null);
		Assert.That(telegram.SentMessages.Count(m => m.UserId == "800" && m.Text.Contains("Пробую зв’язатися з AI-помічником Санти")), Is.EqualTo(1));
		Assert.That(telegram.SentMessages.Any(m => m.UserId == "801" && m.Text.Contains("Пробую зв’язатися з AI-помічником Санти")), Is.False);
		Assert.That(telegram.SentMessages.Any(m => m.Text.Contains("AI-помічник на зв’язку")), Is.True);
		Assert.That(conversationRepo.Created.Count, Is.EqualTo(1));
		Assert.That(telegram.SentMessages.Where(m => m.UserId is "800" or "801").All(m => m.Markup == null), Is.True);
		var santaState = pending.TryGetConversation("800", out var santaConversation) ? santaConversation : null;
		var recipientState = pending.TryGetConversation("801", out var recipientConversation) ? recipientConversation : null;
		Assert.That(santaState?.WizardName, Is.EqualTo("santa_dialog"));
		Assert.That(recipientState?.WizardName, Is.EqualTo("santa_dialog"));

		await orchestrator.EndConversationAsync(eventEntity.EventId, "800", CancellationToken.None);
		Assert.That(conversationRepo.Created.Single().Status, Is.EqualTo("Closed"));
		Assert.That(pending.TryGetConversation("800", out _), Is.False);
		Assert.That(pending.TryGetConversation("801", out _), Is.False);
	}

	[Test]
	public async Task ConversationOrchestrator_GroqUnavailable_UsesDirectAnonymousChat()
	{
		var participantStore = new ConcurrentDictionary<string, Participant>();
		var participantRepo = new FakeParticipantRepository(participantStore);
		var participantService = new ParticipantService(participantRepo);
		var pending = new PendingUserInputService();
		var telegram = new FakeTelegramSender();
		var textGeneration = new UnavailableTextGenerationService();
		var conversationRepo = new FakeConversationRepository();
		var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
		var orchestrator = new ConversationOrchestrator(textGeneration, pending, telegram, participantService, new FakeWizardRegistry(), conversationRepo, config, NullLogger<ConversationOrchestrator>.Instance);
		participantStore["evt-direct:800"] = new Participant { EventId = "evt-direct", TelegramUserId = "800", DisplayName = "Santa" };
		participantStore["evt-direct:801"] = new Participant { EventId = "evt-direct", TelegramUserId = "801", DisplayName = "Recipient" };

		await orchestrator.StartConversationAsync("evt-direct", "800", "801", "800");
		var response = await orchestrator.HandleIncomingMessageAsync("evt-direct", "800", "Привіт, що тобі подобається?");

		Assert.That(textGeneration.CallCount, Is.EqualTo(1));
		Assert.That(telegram.Messages.Count(message => message.UserId == "801" && message.Text.Contains("AI-помічник Санти зараз зайнятий")), Is.EqualTo(1));
		Assert.That(telegram.Messages.Any(message => message.UserId == "801" && message.Text.Contains("AI-помічник Санти зараз зайнятий")), Is.True);
		Assert.That(response, Is.EqualTo("Повідомлення доставлено."));
		Assert.That(telegram.Messages.Any(message => message.UserId == "801" && message.Text.Contains("Анонімне повідомлення від Санти:") && message.Text.Contains("Привіт, що тобі подобається?")), Is.True);
		Assert.That(telegram.Messages.Count(message => message.UserId == "801" && message.Text.Contains("Анонімне повідомлення від Санти:") && message.Text.Contains("Привіт, що тобі подобається?")), Is.EqualTo(1));
		Assert.That(pending.TryGetConversation("800", out var state), Is.True);
		Assert.That(state!.Data["aiEnabled"], Is.EqualTo(bool.FalseString));
		Assert.That(telegram.SentMessages.Where(message => message.Text.Contains("Пробую зв’язатися") || message.Text.Contains("Продовжую стукати"))
			.All(message => message.Markup == null), Is.True);
		Assert.That(telegram.SentMessages.Where(message => message.Text.Contains("AI-помічник Санти зараз зайнятий"))
			.All(message => HasEndConversationButton(message.Markup)), Is.True);
		Assert.That(telegram.SentMessages.Count(message => message.UserId == "800" && message.Text.Contains("AI-помічник Санти зараз зайнятий")), Is.EqualTo(1));
		Assert.That(telegram.SentMessages.Count(message => message.UserId == "801" && message.Text.Contains("AI-помічник Санти зараз зайнятий")), Is.EqualTo(1));
		Assert.That(telegram.SentMessages.Count(message => message.UserId == "800" && message.Text.Contains("Пробую зв’язатися")), Is.EqualTo(1));
		Assert.That(telegram.SentMessages.Any(message => message.UserId == "801" && message.Text.Contains("Пробую зв’язатися")), Is.False);
	}

	private static bool HasEndConversationButton(ReplyMarkup? markup) =>
		markup is InlineKeyboardMarkup keyboard && keyboard.InlineKeyboard.SelectMany(row => row).Any(button => button.CallbackData == "conversation:end");

	private static bool HasPostConversationActions(ReplyMarkup? markup) =>
		markup is InlineKeyboardMarkup keyboard &&
		keyboard.InlineKeyboard.SelectMany(row => row).Select(button => button.CallbackData)
			.ToHashSet()
			.SetEquals(["chat:recipient", "chat:santa"]);

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
		private readonly List<Vote> _votes;
		private readonly List<VoteSession> _sessions = new();

		public FakeVoteRepository(ConcurrentDictionary<string, Vote>? voteStore = null)
		{
			_votes = voteStore is null ? new List<Vote>() : voteStore.Values.ToList();
		}

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

	private sealed class FakeTelegramSender : ITelegramSender
	{
		public List<(string UserId, string Text)> Messages { get; } = new();
		public List<(string UserId, string Text, ReplyMarkup? Markup)> SentMessages { get; } = new();

		public Task SendMessageAsync(string telegramUserId, string text, ReplyMarkup? replyMarkup = null)
		{
			Messages.Add((telegramUserId, text));
			SentMessages.Add((telegramUserId, text, replyMarkup));
			return Task.CompletedTask;
		}

		public Task AnswerCallbackQueryAsync(string callbackQueryId, string? text = null) => Task.CompletedTask;
	}

	private sealed class FakeTextGenerationService : ITextGenerationService
	{
		public string LastPrompt { get; private set; } = string.Empty;

		public Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default, Func<CancellationToken, Task>? onSecondAttempt = null)
		{
			LastPrompt = prompt;
			return Task.FromResult("Привіт! Я хочу уточнити, що саме тобі більше подобається.");
		}
	}

	private sealed class UnavailableTextGenerationService : ITextGenerationService
	{
		public int CallCount { get; private set; }

		public Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default, Func<CancellationToken, Task>? onSecondAttempt = null)
		{
			CallCount++;
			throw new HttpRequestException("Groq unavailable", null, System.Net.HttpStatusCode.ServiceUnavailable);
		}
	}

	private sealed class FakeConversationRepository : IConversationRepository
	{
		public List<Conversation> Created { get; } = new();

		public Task CreateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
		{
			Created.Add(conversation);
			return Task.CompletedTask;
		}

		public Task<Conversation?> GetByRecipientAsync(string eventId, string recipientTelegramUserId, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(Created.FirstOrDefault(c => c.EventId == eventId && c.RecipientTelegramUserId == recipientTelegramUserId));
		}

		public Task<Conversation?> GetActiveForParticipantAsync(string eventId, string telegramUserId, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(Created.FirstOrDefault(c => c.EventId == eventId && c.Status == "Active" && (c.SantaTelegramUserId == telegramUserId || c.RecipientTelegramUserId == telegramUserId)));
		}

		public Task AddMessageAsync(ConversationMessage message, CancellationToken cancellationToken = default)
		{
			return Task.CompletedTask;
		}

		public Task UpdateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
		{
			return Task.CompletedTask;
		}
	}

	private sealed class FakeWizardRegistry : IWizardRegistry
	{
		public IReadOnlyList<(string FieldName, string Prompt)> GetSteps(string wizardName)
		{
			if (string.Equals(wizardName, "wishlist", StringComparison.OrdinalIgnoreCase))
			{
				return new List<(string FieldName, string Prompt)>
				{
					("WishlistText", "Напишіть ваші побажання щодо подарунку:"),
					("Hobbies", "Напишіть ваші хобі:"),
					("PreferredGiftCategories", "Які категорії подарунків вам подобаються?")
				};
			}

			return Array.Empty<(string FieldName, string Prompt)>();
		}
	}
}
