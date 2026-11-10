# Cosmos DB setup

Provision the user-session container in the existing Cosmos DB SQL database before running the application:

```powershell
az deployment group create --resource-group <resource-group> --template-file infra/cosmos-user-event-sessions.bicep --parameters cosmosAccountName=<cosmos-account-name>
```

The template creates `UserEventSessions` in `SecretSantaDb` with `/telegramUserId` as its partition key, matching `CosmosUserEventSessionRepository`. If the database has another name, pass `databaseName` explicitly. The deployment identity needs permission to manage Cosmos DB SQL containers; the application identity does not need container-management permissions.
