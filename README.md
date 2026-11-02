# Ticket System

IT support ticket system: users report problems, support staff resolve them, admins see how fast problems are solved.

> Work in progress. The full README (setup guide, default accounts, API overview) is added in issue #25.

## Planned components

| Component | Technology | Folder |
|---|---|---|
| Web API | ASP.NET Core (C#), EF Core, SQLite, JWT, WebSocket | `src/TicketSystem.Api` |
| Background service | .NET Worker Service | `src/TicketSystem.Worker` |
| Web client | React (Vite) | `src/TicketSystem.Client` |
| Tests | xUnit, Moq | `tests/TicketSystem.Tests` |

## Repository layout

```
docs/      documentation, Postman collection, .http requests
src/       API, Worker and React client
tests/     unit tests
```
