# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction
- Before creating a new file, check existing namespaces and folder structures instead of using a generic path.
- Prefer editing existing files over creating new ones when modifications are needed. Double check folder structure and naming conventions before creating new files.
- When a NuGet project is modified, it must be repacked and published, and all consumer projects must update their NuGet packages. Do not add project references to domain projects.

## Code Style
- Use specific formatting rules
- Follow naming conventions

## Project-Specific Rules
- Use ServiceDefaults-based conditional bus configuration in Program.cs instead of manual RabbitMQ/Azure Service Bus setup, matching other Program.cs patterns in this repo.

## Testing Guidelines
- Prefer AutoData tests to inject event objects directly to keep parameters minimal and improve readability (e.g., reservation).
- Add Arrange/Act/Assert comments and explanatory comments for complex lines to enhance test clarity.