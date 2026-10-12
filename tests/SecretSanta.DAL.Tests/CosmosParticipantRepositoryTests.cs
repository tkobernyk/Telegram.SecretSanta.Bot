using Microsoft.Azure.Cosmos;
using NSubstitute;
using NUnit.Framework;
using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;
using SecretSanta.DAL.Repositories;

namespace SecretSanta.DAL.Tests
{
	public class CosmosParticipantRepositoryTests
	{
		[Test]
		public async Task GetByTelegramUserIdAsync_ReturnsParticipant_WhenExists()
		{
			var participant = new Participant { EventId = "evt-1", TelegramUserId = "tg-123", DisplayName = "Test" };
			var fake = Substitute.For<IExecutor>();
			fake.GetItemByIdAsync<Participant>(Arg.Any<QueryDefinition>(), "evt-1", Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(participant));

			var repo = new CosmosParticipantRepository(fake);

			var result = await repo.GetByTelegramUserIdAsync("evt-1", "tg-123");

			Assert.NotNull(result);
			Assert.AreEqual(participant.TelegramUserId, result!.TelegramUserId);
			Assert.AreEqual(participant.EventId, result.EventId);
		}

		[Test]
		public async Task GetByTelegramUserIdAsync_ReturnsNull_WhenNotFound()
		{
			var fake = Substitute.For<IExecutor>();
			fake.GetItemByIdAsync<Participant>(Arg.Any<QueryDefinition>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(await Task.FromResult<Participant>(default!));

			var repo = new CosmosParticipantRepository(fake);

			var result = await repo.GetByTelegramUserIdAsync("evt-1", "tg-999");

			Assert.IsNull(result);
		}

		[Test]
		public async Task CreateAsync_Throws_WhenEventIdMissing()
		{
			var fake = Substitute.For<IExecutor>();
			var repo = new CosmosParticipantRepository(fake);
			var participant = new Participant { EventId = string.Empty, TelegramUserId = "tg-1" };

			await Assert.ThrowsAsync<ArgumentException>(async () => await repo.CreateAsync(participant));
		}

		[Test]
		public async Task CreateAsync_CallsExecutorCreate_WhenValid()
		{
			var fake = Substitute.For<IExecutor>();
			fake.CreateItemAsync(Arg.Any<Participant>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(call => Task.FromResult(call.ArgAt<Participant>(0)));

			var repo = new CosmosParticipantRepository(fake);
			var participant = new Participant { EventId = "evt-1", TelegramUserId = "tg-1" };

			await repo.CreateAsync(participant);

			await fake.Received(1).CreateItemAsync(Arg.Is<Participant>(p => p == participant), participant.EventId, Arg.Any<CancellationToken>());
		}
	}
}
