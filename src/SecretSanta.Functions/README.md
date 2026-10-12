SecretSanta.Functions - Phase 1 skeleton

This project contains an Azure Functions (isolated worker) host skeleton for the Secret Santa bot.

Quick start (local)

1. Add the project to your solution:
   dotnet sln D:\Projects\Telegram.SecretSanta.Bot\Telegram.SecretSanta.Bot.slnx add src/SecretSanta.Functions/SecretSanta.Functions.csproj

2. Populate local settings:
   - Copy local.settings.json.example to local.settings.json in the same folder and fill values.

3. Build:
   dotnet build src/SecretSanta.Functions/SecretSanta.Functions.csproj

4. Run locally with the Functions Core Tools (recommended):
   - Install: https://learn.microsoft.com/azure/azure-functions/functions-run-local
   - Start: func start --project src/SecretSanta.Functions

   Or run with dotnet (for quick test):
   dotnet run --project src/SecretSanta.Functions/SecretSanta.Functions.csproj

Notes
- The project targets net10.0 and uses the .NET isolated worker.
- Program.cs performs minimal configuration validation and will fail on startup if required environment variables are missing.
- The Telegram webhook function is a placeholder and only validates the webhook secret header.
- Do not commit local.settings.json with real secrets.

Next steps
- Implement Cosmos DB integration and repositories
- Implement participant registration and commands
- Add unit and integration tests

