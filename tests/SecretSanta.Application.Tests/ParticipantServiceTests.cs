using System.Threading.Tasks;
using Xunit;
using SecretSanta.Domain.Participants;
using SecretSanta.Domain.Models;
using SecretSanta.Infrastructure.Participants;
using System.Threading;
using SecretSanta.Domain.Entities;

namespace SecretSanta.Application.Tests
{
	public class ParticipantServiceTests
	{
		[Fact]
		public async Task CreateAndGetParticipant()
		{
			var repo = new InMemoryParticipantRepository();
			var service = new ParticipantService(repo);

			var participant = new ParticipantDto
			{
				EventId = "test-event",
				TelegramUserId = "12345",
				DisplayName = "Test User"
			};

			await service.CreateAsync(participant, CancellationToken.None);

			var fetched = await service.GetByTelegramUserIdAsync("test-event", "12345", CancellationToken.None);

			Assert.NotNull(fetched);
			Assert.Equal("Test User", fetched.DisplayName);
			Assert.Equal(participant.TelegramUserId, fetched.TelegramUserId);
		}
	}
}
