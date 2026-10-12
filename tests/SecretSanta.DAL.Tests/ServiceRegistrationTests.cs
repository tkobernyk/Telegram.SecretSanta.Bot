using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SecretSanta.DAL.Abstractions;
using SecretSanta.DAL.Cosmos;
using SecretSanta.DAL.Repositories;

namespace SecretSanta.DAL.Tests
{
	public class ServiceRegistrationTests
	{
		[Test]
		public void AddSecretSantaDal_RegistersExpectedServices()
		{
			var services = new ServiceCollection();
			var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

			// Act
			ServiceCollectionExtensions.AddSecretSantaDal(services, config);

			// Assert - check descriptors exist
			Assert.IsTrue(services.Any(d => d.ServiceType == typeof(CosmosClientFactory)));
			Assert.IsTrue(services.Any(d => d.ServiceType == typeof(IParticipantRepository)));
			Assert.IsTrue(services.Any(d => d.ImplementationType == typeof(CosmosParticipantRepository) || d.ImplementationFactory != null));
		}
	}
}
