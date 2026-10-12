using System;
using Microsoft.Extensions.Configuration;
using Xunit;
using SecretSanta.DAL.Cosmos;

namespace SecretSanta.DAL.Tests
{
	public class CosmosClientFactoryTests
	{
		[Fact]
		public void Ctor_Throws_When_ConfigurationMissing()
		{
			var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
			Assert.Throws<ArgumentException>(() => new CosmosClientFactory(config));
		}

		[Fact]
		public void Ctor_Succeeds_With_EndpointAndKey_Placeholders()
		{
			// Do not attempt to connect; provide placeholder values to verify construction path that doesn't validate network connectivity synchronously
			var inMemory = new System.Collections.Generic.Dictionary<string, string?>
			{
				["Cosmos__Endpoint"] = "https://localhost:8081",
				["Cosmos__Key"] = "C2F...fakeKey"
			};

			var config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();

			// Construction should succeed (may not validate connectivity until used)
			var factory = new CosmosClientFactory(config);
			Assert.NotNull(factory);
			Assert.NotNull(factory.Client);
		}
	}
}
