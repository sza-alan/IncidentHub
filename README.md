# IncidentHub

API para gerenciamento e análise de incidentes, desenvolvida em **.NET 10** como projeto de estudo e portfólio.

## Tecnologias

- .NET 10 / ASP.NET Core
- Entity Framework Core
- SQLite
- MediatR / CQRS
- Clean Architecture
- xUnit
- HttpClient + políticas de resiliência
- Ollama com modelo local
- Tool Calling
- MCP
- Retrieval com runbooks locais

## Funcionalidades

- Cadastro e consulta de incidentes
- Filtros e paginação
- Alteração de status
- Consulta de health de serviços externos
- Retry e timeout em integrações HTTP
- Análise de incidentes com LLM local
- Tool Calling para consulta de health
- MCP Server via `stdio`
- Contexto adicional através de runbooks
- Testes unitários e de integração

## Estrutura

```text
src/
├── IncidentHub.Api
├── IncidentHub.Application
├── IncidentHub.Domain
├── IncidentHub.Infrastructure
└── IncidentHub.Mcp

tests/
├── IncidentHub.UnitTests
└── IncidentHub.IntegrationTests
