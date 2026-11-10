using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SecretSanta.Infrastructure.AI;

namespace SecretSanta.Domain.Tests;

public class GroqServiceTests
{
	[TestCase("Groq__ApiKey", "Groq__Model")]
	[TestCase("Groq:ApiKey", "Groq:Model")]
	[TestCase("Values:Groq__ApiKey", "Values:Groq__Model")]
	public async Task GenerateResponseAsync_ReadsConfigurationAndSendsGroqChatCompletionRequest(string keyName, string modelName)
	{
		var handler = new RecordingHandler("{\"choices\":[{\"message\":{\"content\":\"Привіт!\"}}]}");
		var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.groq.com/openai/v1/") };
		var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			[keyName] = "test-key",
			[modelName] = "llama-test"
		}).Build();
		var service = new GroqService(new TestHttpClientFactory(client), configuration, NullLogger<GroqService>.Instance);

		var result = await service.GenerateResponseAsync("Тестовий запит");

		Assert.That(result, Is.EqualTo("Привіт!"));
		Assert.That(handler.RequestUri!.AbsolutePath, Is.EqualTo("/openai/v1/chat/completions"));
		Assert.That(handler.ApiKey, Is.EqualTo("test-key"));
		using var requestJson = System.Text.Json.JsonDocument.Parse(handler.RequestBody);
		Assert.That(requestJson.RootElement.GetProperty("model").GetString(), Is.EqualTo("llama-test"));
		Assert.That(requestJson.RootElement.GetProperty("messages")[0].GetProperty("content").GetString(), Is.EqualTo("Тестовий запит"));
	}

	[Test]
	public void GenerateResponseAsync_ThrowsWhenApiKeyIsMissing()
	{
		var service = new GroqService(new TestHttpClientFactory(new HttpClient()), new ConfigurationBuilder().Build(), NullLogger<GroqService>.Instance);

		Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateResponseAsync("Тест"));
	}

	[Test]
	public async Task GenerateResponseAsync_RetriesServiceUnavailableAndReturnsSuccessfulResponse()
	{
		var handler = new SequenceHandler(
			new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
			new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"Готово\"}}]}", Encoding.UTF8, "application/json") });
		var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.groq.com/openai/v1/") };
		var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["Groq:ApiKey"] = "test-key",
			["Groq:Model"] = "llama-test"
		}).Build();
		var service = new GroqService(new TestHttpClientFactory(client), configuration, NullLogger<GroqService>.Instance);
		var retryNotifications = 0;

		var result = await service.GenerateResponseAsync("Тест", onSecondAttempt: _ =>
		{
			retryNotifications++;
			return Task.CompletedTask;
		});

		Assert.That(result, Is.EqualTo("Готово"));
		Assert.That(handler.RequestCount, Is.EqualTo(2));
		Assert.That(retryNotifications, Is.EqualTo(1));
	}

	private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
	{
		public HttpClient CreateClient(string name) => client;
	}

	private sealed class RecordingHandler(string responseBody) : HttpMessageHandler
	{
		public Uri? RequestUri { get; private set; }
		public string? ApiKey { get; private set; }
		public string RequestBody { get; private set; } = string.Empty;

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestUri = request.RequestUri;
			ApiKey = request.Headers.Authorization!.Parameter;
			RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
			return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseBody, Encoding.UTF8, "application/json") };
		}
	}

	private sealed class SequenceHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
	{
		private readonly Queue<HttpResponseMessage> _responses = new(responses);
		public int RequestCount { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestCount++;
			return Task.FromResult(_responses.Dequeue());
		}
	}
}
