using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SecretSanta.Functions.Functions
{
	public class HealthFunction
	{
		private readonly ILogger<HealthFunction> _logger;

		public HealthFunction(ILogger<HealthFunction> logger)
		{
			_logger = logger;
		}

		[Function("Health")]
		public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
		{
			_logger.LogInformation("Health check requested.");

			var response = req.CreateResponse(HttpStatusCode.OK);
			response.Headers.Add("Content-Type", "application/json; charset=utf-8");

			var payload = new
			{
				status = "Healthy",
				timestampUtc = DateTime.UtcNow.ToString("o"),
				runtime = "dotnet-isolated",
				targetFramework = "net10.0"
			};

			await response.WriteStringAsync(JsonSerializer.Serialize(payload));
			return response;
		}
	}
}
