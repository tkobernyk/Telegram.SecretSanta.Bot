using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SecretSanta.DAL.Cosmos;
using SecretSanta.DAL.Entities;

namespace SecretSanta.DAL.Tests
{
	public class CosmosClientFactoryTests
	{
		[Test]
		public void Ctor_Throws_When_ConfigurationMissing()
		{
			var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
			Assert.Throws<ArgumentException>(() => new CosmosClientProvider(config));
		}

		[Test]
		public void Ctor_Succeeds_With_EndpointAndKey_Placeholders()
		{
			var inMemory = new Dictionary<string, string?>
			{
				["Cosmos:Endpoint"] = "https://localhost:8081",
				["Cosmos:Key"] = "ZmFrZUtleQ==",
				["Cosmos:DatabaseName"] = "SecretSantaDb"
			};

			var config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();

			// Construction should succeed (may not validate connectivity until used)
			var provider = new CosmosClientProvider(config);
			var factory = new CosmosClientFactory<SecretSanta.DAL.Entities.Participant>(provider);
			Assert.NotNull(factory);
			Assert.NotNull(factory.GetContainer());
		}
	}
}
