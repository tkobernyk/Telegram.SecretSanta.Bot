# Copilot Instructions

## Project Guidelines
- Tests for the business logic should live in the Domain test project, not in the DAL test project.
- Follow a strict layered architecture with Azure Functions only as a Telegram webhook adapter.
- Place all business logic in the Domain layer.
- Implement all data access and Cosmos models/repositories in the DAL via an IDbExecutor abstraction.
- Use services for calculations and external API handlers.
- Ensure each class, interface, enum, and struct is in a separate file and grouped by folders.
- Create a generic Cosmos client factory by model type as a generic class.