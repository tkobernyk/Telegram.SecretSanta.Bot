using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Entities;
using SecretSanta.DAL.Repositories;
using Xunit;

namespace SecretSanta.DAL.Tests
{
	public class CosmosParticipantRepositoryTests
	{
		[Fact]
		public async Task GetByTelegramUserIdAsync_ReturnsParticipant_WhenExists()
		{
			var participant = new Participant { EventId = "evt-1", TelegramUserId = "tg-123", DisplayName = "Test" };
			var fake = new FakeExecutor();
			fake.PreconfiguredParticipant = participant;

			var repo = new CosmosParticipantRepository(fake);

			var result = await repo.GetByTelegramUserIdAsync("evt-1", "tg-123");

			Assert.NotNull(result);
			Assert.Equal(participant.TelegramUserId, result!.TelegramUserId);
			Assert.Equal(participant.EventId, result.EventId);
		}

		[Fact]
		public async Task GetByTelegramUserIdAsync_ReturnsNull_WhenNotFound()
		{
			var fake = new FakeExecutor();
			var repo = new CosmosParticipantRepository(fake);

			var result = await repo.GetByTelegramUserIdAsync("evt-1", "tg-999");

			Assert.Null(result);
		}

		[Fact]
		public async Task CreateAsync_Throws_WhenEventIdMissing()
		{
			var fake = new FakeExecutor();
			var repo = new CosmosParticipantRepository(fake);
			var participant = new Participant { EventId = string.Empty, TelegramUserId = "tg-1" };

			await Assert.ThrowsAsync<ArgumentException>(() => repo.CreateAsync(participant));
		}

		[Fact]
		public async Task CreateAsync_CallsExecutorCreate_WhenValid()
		{
			var fake = new FakeExecutor();
			var repo = new CosmosParticipantRepository(fake);
			var participant = new Participant { EventId = "evt-1", TelegramUserId = "tg-1" };

			await repo.CreateAsync(participant);

			Assert.True(fake.CreateCalled);
			Assert.Equal(participant.EventId, fake.LastEventIdPassedToCreate);
		}

		private class FakeExecutor : IExecutor
		{
			public Participant? PreconfiguredParticipant;
			public bool CreateCalled;
			public string? LastEventIdPassedToCreate;

			public Task<T> GetItemByIdAsync<T>(QueryDefinition query, string eventId, CancellationToken cancellationToken)
			{
				if (PreconfiguredParticipant is object && typeof(T) == typeof(Participant))
				{
					// if event id matches or no constraint, return preconfigured
					if (PreconfiguredParticipant.EventId == eventId)
					{
						return Task.FromResult((T)(object)PreconfiguredParticipant);
					}
				}

				return Task.FromResult(default(T)!);
			}

			public Task<T> CreateItemAsync<T>(T item, string eventId, CancellationToken cancellationToken)
			{
				CreateCalled = true;
				LastEventIdPassedToCreate = eventId;
				return Task.FromResult(item);
			}
		}
	}
}
