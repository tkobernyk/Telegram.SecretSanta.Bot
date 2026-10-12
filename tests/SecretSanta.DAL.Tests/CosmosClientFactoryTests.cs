using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SecretSanta.DAL.Cosmos;

namespace SecretSanta.DAL.Tests
{
	public class CosmosClientFactoryTests
	{
		[Test]
		public void Ctor_Throws_When_ConfigurationMissing()
		{
			var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
			Assert.Throws<ArgumentException>(() => new CosmosClientFactory(config));
		}

		[Test]
		public void Ctor_Succeeds_With_EndpointAndKey_Placeholders()
		{
			var inMemory = new Dictionary<string, string?>
			{
				["Cosmos:Endpoint"] = "https://localhost:8081",
				["Cosmos:Key"] = "ZmFrZUtleQ=="
			};

			var config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();

			// Construction should succeed (may not validate connectivity until used)
			var factory = new CosmosClientFactory(config);
			Assert.NotNull(factory);
			Assert.NotNull(factory.Client);
		}
	}
}
