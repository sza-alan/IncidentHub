# IncidentHub

Backend project built with **.NET 10** for incident management, external service health checks and local AI-assisted incident analysis.

The project was created as a practical study of backend architecture, resilient integrations, testing and AI-related concepts such as structured output, Tool Calling, MCP and retrieval.

---

## Features

- Incident creation and management
- Incident filtering and pagination
- Incident status transitions
- External service health checks
- Resilient HTTP integrations
- Local incident analysis using Ollama
- Structured LLM responses
- Tool Calling for service health checks
- MCP Server with `stdio`
- Local runbook retrieval
- Unit and integration tests

---

## Tech Stack

### Backend

- .NET 10
- ASP.NET Core
- C#
- Entity Framework Core
- SQLite
- MediatR

### AI

- Ollama
- qwen2.5:3b
- Microsoft.Extensions.AI
- Tool Calling
- Model Context Protocol (MCP)
- Local runbook retrieval

### Testing

- xUnit
- Integration testing
- Stubbed external dependencies

### Resilience

- `IHttpClientFactory`
- Retry
- Timeout
- Exponential backoff
- Jitter
- CancellationToken propagation

---

## Architecture

The solution follows a layered architecture inspired by Clean Architecture.

```text
src
├── IncidentHub.Api
├── IncidentHub.Application
├── IncidentHub.Domain
├── IncidentHub.Infrastructure
└── IncidentHub.Mcp

tests
├── IncidentHub.UnitTests
└── IncidentHub.IntegrationTests
