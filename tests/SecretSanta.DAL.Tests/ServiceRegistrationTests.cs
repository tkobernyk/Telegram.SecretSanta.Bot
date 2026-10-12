using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SecretSanta.DAL.Tests
{
	public class ServiceRegistrationTests
	{
		[Fact]
		public void AddSecretSantaDal_RegistersExpectedServices()
		{
			var services = new ServiceCollection();
			var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

			// Act
			SecretSanta.DAL.ServiceCollectionExtensions.AddSecretSantaDal(services, config);

			// Assert - check descriptors exist
			Assert.Contains(services, d => d.ServiceType == typeof(SecretSanta.DAL.Cosmos.CosmosClientFactory));
			Assert.Contains(services, d => d.ServiceType == typeof(SecretSanta.DAL.Abstractions.ICosmosContainerProvider));
			Assert.Contains(services, d => d.ServiceType == typeof(SecretSanta.DAL.Abstractions.IParticipantRepository));
			Assert.Contains(services, d => d.ImplementationType == typeof(SecretSanta.DAL.Repositories.CosmosParticipantRepository) || d.ImplementationFactory != null);
		}
	}
}
