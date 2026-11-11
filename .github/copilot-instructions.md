# Copilot Instructions

## Project Guidelines
- Tests for the business logic should live in the Domain test project, not in the DAL test project.
- Follow a strict layered architecture with Azure Functions only as a Telegram webhook adapter.
- Place all business logic in the Domain layer.
- Implement all data access and Cosmos models/repositories in the DAL via an IDbExecutor abstraction.
- Use services for calculations and external API handlers. Services must be pure API wrappers, calculations, or manager-like orchestrators, and must not depend on repositories; repository interactions should be in handlers.
- Ensure each class, interface, enum, and struct is in a separate file and grouped by folders.
- Create a generic Cosmos client factory by model type as a generic class.
- State transitions should be triggered via MediatR Send commands, and TelegramEndpointHandler should use enum-based callback routing with prefix parsing and avoid long if/else chains.

## AI-mediated Chat UX Guidelines
- Send a message to the counterparty only on explicit 'Send' button press.
- Provide a short AI summary before ending the dialog.
- The 'End dialog' button should be available only to the initiator.
- Role names should be 'Миколайко / Отримувач'.