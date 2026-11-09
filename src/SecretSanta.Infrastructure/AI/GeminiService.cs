using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace SecretSanta.Infrastructure.AI;

public class GeminiService : IGeminiService
{
	readonly IHttpClientFactory _httpFactory;
	readonly ILogger<GeminiService> _logger;
	readonly string _apiKey;
	readonly string _model;

	public GeminiService(IHttpClientFactory httpFactory, IConfiguration configuration, ILogger<GeminiService> logger)
	{
		_httpFactory = httpFactory;
		_logger = logger;
		_apiKey = configuration["Values:Gemini:ApiKey"] ?? string.Empty;
		_model = configuration["Values:Gemini:Model"] ?? "models/text-bison-001";
	}

	public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(_apiKey))
		{
			_logger.LogWarning("Gemini API key is not configured.");
			throw new InvalidOperationException("Gemini API key is not configured.");
		}

		var client = _httpFactory.CreateClient("gemini");

		var payload = new
		{
			prompt = new { text = prompt },
			maxOutputTokens = 512,
			temperature = 0.7
		};

		var url = $"v1/{_model}:generateText?key={_apiKey}";

		HttpResponseMessage resp;
		try
		{
			resp = await client.PostAsJsonAsync(url, payload, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error sending request to Gemini");
			throw;
		}

		if (!resp.IsSuccessStatusCode)
		{
			var body = await resp.Content.ReadAsStringAsync(cancellationToken);
			_logger.LogError("Gemini returned status {Status} body={Body}", resp.StatusCode, body);
			resp.EnsureSuccessStatusCode();
		}

		using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
		var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

		// Expecting response shape with candidates[].content[].text (Generative Language API)
		if (json.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
		{
			var first = candidates[0];
			if (first.TryGetProperty("content", out var content) && content.GetArrayLength() > 0)
			{
				var text = content[0].GetProperty("text").GetString();
				return text ?? string.Empty;
			}
		}

		if (json.RootElement.TryGetProperty("output", out var output) && output.TryGetProperty("text", out var outText))
		{
			return outText.GetString() ?? string.Empty;
		}

		return string.Empty;
	}
}
