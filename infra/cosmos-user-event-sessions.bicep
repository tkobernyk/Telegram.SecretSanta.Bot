// Provisions the UserEventSessions container in an existing Cosmos DB SQL database.
// The application runtime identity does not need container-management permissions.
param cosmosAccountName string
param databaseName string = 'SecretSantaDb'
param containerName string = 'UserEventSessions'

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: cosmosAccountName
}

resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' existing = {
  parent: cosmosAccount
  name: databaseName
}

resource userEventSessions 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDatabase
  name: containerName
  properties: {
	resource: {
	  id: containerName
	  partitionKey: {
		paths: [
		  '/telegramUserId'
		]
		kind: 'Hash'
		version: 2
	  }
	}
  }
}
