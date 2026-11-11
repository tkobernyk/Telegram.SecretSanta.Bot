using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace SecretSanta.Infrastructure.AI;

public class GroqService : ITextGenerationService
{
	const int MaxAttempts = 3;
	static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(250);
	static readonly TimeSpan MaxRetryAfter = TimeSpan.FromSeconds(2);
	readonly IHttpClientFactory _httpFactory;
	readonly ILogger<GroqService> _logger;
	readonly string _apiKey;
	readonly string _model;

	public GroqService(IHttpClientFactory httpFactory, IConfiguration configuration, ILogger<GroqService> logger)
	{
		_httpFactory = httpFactory;
		_logger = logger;
		_apiKey = GetConfigurationValue(configuration, "Groq:ApiKey", "Groq__ApiKey", "Values:Groq__ApiKey", "Values:Groq:ApiKey");
		_model = GetConfigurationValue(configuration, "Groq:Model", "Groq__Model", "Values:Groq__Model", "Values:Groq:Model");
		if (string.IsNullOrWhiteSpace(_model))
		{
			_model = "llama-3.3-70b-versatile";
		}
		_model = _model.Trim();
	}

	private static string GetConfigurationValue(IConfiguration configuration, params string[] keys)
	{
		foreach (var key in keys)
		{
			var value = configuration[key];
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
		}

		return string.Empty;
	}

	public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default, Func<CancellationToken, Task>? onSecondAttempt = null)
	{
		if (string.IsNullOrEmpty(_apiKey))
		{
			_logger.LogWarning("Groq API key is not configured.");
			throw new InvalidOperationException("Groq API key is not configured.");
		}

		var client = _httpFactory.CreateClient("groq");
		var payload = JsonSerializer.Serialize(new
		{
			model = _model,
			messages = new[]
			{
				new { role = "user", content = prompt }
			},
			max_completion_tokens = 512,
			temperature = 0.7
		});
		for (var attempt = 1; attempt <= MaxAttempts; attempt++)
		{
			using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
			{
				Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
			};
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
			using var resp = await client.SendAsync(request, cancellationToken);
			if (resp.IsSuccessStatusCode)
			{
				using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
				using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
				return GetGeneratedText(json);
			}

			if (!IsTransientStatusCode(resp.StatusCode) || attempt == MaxAttempts)
			{
				_logger.LogError("Groq returned status {StatusCode} after {AttemptCount} attempt(s).", (int)resp.StatusCode, attempt);
				throw new HttpRequestException($"Groq request failed with status code {(int)resp.StatusCode}.", null, resp.StatusCode);
			}

			var retryDelay = GetRetryDelay(resp, attempt);
			_logger.LogWarning("Groq returned transient status {StatusCode}; retrying in {RetryDelayMs} ms (attempt {Attempt}/{MaxAttempts}).", (int)resp.StatusCode, retryDelay.TotalMilliseconds, attempt, MaxAttempts);
			if (attempt == 1 && onSecondAttempt != null)
			{
				await onSecondAttempt(cancellationToken);
			}
			await Task.Delay(retryDelay, cancellationToken);
		}

		throw new InvalidOperationException("Groq request ended without a successful response.");
	}

	private static bool IsTransientStatusCode(System.Net.HttpStatusCode statusCode) =>
		statusCode is System.Net.HttpStatusCode.RequestTimeout or System.Net.HttpStatusCode.TooManyRequests or
		System.Net.HttpStatusCode.InternalServerError or System.Net.HttpStatusCode.BadGateway or
		System.Net.HttpStatusCode.ServiceUnavailable or System.Net.HttpStatusCode.GatewayTimeout;

	private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
	{
		var retryAfter = response.Headers.RetryAfter;
		var delay = retryAfter?.Delta ?? (retryAfter?.Date is { } retryDate ? retryDate - DateTimeOffset.UtcNow : TimeSpan.FromMilliseconds(250 * attempt));
		return delay < TimeSpan.Zero ? TimeSpan.Zero : delay > MaxRetryAfter ? MaxRetryAfter : delay;
	}

	private static string GetGeneratedText(JsonDocument json)
	{
		if (json.RootElement.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
		{
			foreach (var choice in choices.EnumerateArray())
			{
				if (choice.TryGetProperty("message", out var message))
				{
					if (message.TryGetProperty("content", out var content))
					{
						var extracted = ExtractText(content);
						if (!string.IsNullOrWhiteSpace(extracted))
						{
							return extracted!;
						}
					}

					if (message.TryGetProperty("reasoning", out var reasoning) && reasoning.ValueKind == JsonValueKind.String)
					{
						var reasoningText = reasoning.GetString();
						if (!string.IsNullOrWhiteSpace(reasoningText))
						{
							return reasoningText!;
						}
					}
				}

				if (choice.TryGetProperty("text", out var text))
				{
					var extracted = ExtractText(text);
					if (!string.IsNullOrWhiteSpace(extracted))
					{
						return extracted!;
					}
				}
			}
		}

		if (json.RootElement.TryGetProperty("output_text", out var outputText))
		{
			var extracted = ExtractText(outputText);
			if (!string.IsNullOrWhiteSpace(extracted))
			{
				return extracted!;
			}
		}

		throw new InvalidOperationException("Groq response did not contain generated text.");
	}

	private static string? ExtractText(JsonElement element)
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.String:
				return element.GetString();
			case JsonValueKind.Array:
			{
				var sb = new StringBuilder();
				foreach (var item in element.EnumerateArray())
				{
					var part = ExtractText(item);
					if (!string.IsNullOrWhiteSpace(part))
					{
						if (sb.Length > 0)
						{
							sb.Append('\n');
						}
						sb.Append(part);
					}
				}
				return sb.Length == 0 ? null : sb.ToString();
			}
			case JsonValueKind.Object:
				if (element.TryGetProperty("text", out var textProp))
				{
					return ExtractText(textProp);
				}
				if (element.TryGetProperty("content", out var contentProp))
				{
					return ExtractText(contentProp);
				}
				break;
		}

		return null;
	}
}
