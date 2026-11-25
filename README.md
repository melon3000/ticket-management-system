# Ticket System

IT support ticket system. Users report problems, support staff take the tickets into work and resolve them, administrators follow the deadlines and see how fast problems are solved. A background service escalates late tickets, and support staff see new and changed tickets in real time.

Final project of the courses Programmeerimine and Hajusrakendused (TechnoTLN, topic 2: ticket system).

## Features

- Four roles: **User**, **Support**, **Admin** and **Service** (the account of the background service).
- Create, edit, assign and delete tickets. A ticket has a title, description, category, priority, state, author, assignee and deadline.
- The deadline comes from the priority: Low 72 h, Medium 24 h, High 8 h, Critical 4 h.
- States: New, InProgress, Resolved. Only support staff take a ticket into work; the assignee or an administrator changes the state.
- Comments on tickets and a change log (created, assigned, state changed, escalated).
- Filtering by state, priority, category and assignee, free-text search, sorting and paging.
- Average resolution time per category and per support person (administrator).
- Worker Service: every 15 minutes it asks for overdue tickets and raises the priority of each one by one level.
- WebSocket `/ws/tickets`: support staff and administrators receive `TicketCreated` and `TicketUpdated` messages.
- React client in Estonian.

## Technology

| Component | Technology | Folder |
|---|---|---|
| Web API | ASP.NET Core (.NET 10), EF Core, SQLite, JWT, WebSocket, Swagger | `src/TicketSystem.Api` |
| Background service | .NET Worker Service | `src/TicketSystem.Worker` |
| Web client | React 19, TypeScript, Vite | `src/TicketSystem.Client` |
| Tests | xUnit, Moq | `tests/TicketSystem.Tests` |

The API has the layers controller, service, repository and `AppDbContext`. Errors are returned as ProblemDetails.

```
docs/      ER diagram, Postman collection
scripts/   check-api.ps1, an end-to-end check of the API rules
src/       API, Worker and React client
tests/     unit tests
```

The ER diagram is in [`docs/er-diagram.png`](docs/er-diagram.png).

## Requirements

- .NET SDK 10 (see `global.json`)
- Node.js 20 or newer and npm
- PowerShell (only for `scripts/check-api.ps1`)

## Running the system

Open three terminals in the repository folder.

### 1. API

```powershell
cd src\TicketSystem.Api
dotnet run
```

The API listens on `http://localhost:5119`. On the first start it creates the SQLite database `ticketsystem.db` (migrations are applied automatically) and adds the starting data: categories, priorities, the accounts below and example tickets. In the Development environment the Swagger page is available at `http://localhost:5119/swagger`.

### 2. Worker Service

```powershell
cd src\TicketSystem.Worker
dotnet run
```

The Worker logs in as the Service account, and every `IntervalMinutes` minutes (default 15) it calls `GET /api/tickets/overdue` and `PATCH /api/tickets/{id}/escalate`. The address of the API and the interval are in `src/TicketSystem.Worker/appsettings.json` (`TicketApi:BaseUrl`, `TicketApi:IntervalMinutes`). Start the API first.

### 3. Web client

```powershell
cd src\TicketSystem.Client
npm install
npm run dev
```

Open `http://localhost:5173`. The client expects the API at `http://localhost:5119`. For another address create `src/TicketSystem.Client/.env.local` with `VITE_API_URL=http://host:port`. The API allows requests from `http://localhost:5173` (`Cors:AllowedOrigins` in `appsettings.json`).

Other client commands: `npm run lint` and `npm run build`.

## Starting accounts

All accounts have the password `Password123!`. They exist for development only.

| Email | Name | Role |
|---|---|---|
| `kati.kask@ticketsystem.local` | Kati Kask | User |
| `toomas.mets@ticketsystem.local` | Toomas Mets | User |
| `mari.maasikas@ticketsystem.local` | Mari Maasikas | Support |
| `jaan.tamm@ticketsystem.local` | Jaan Tamm | Support |
| `admin@ticketsystem.local` | Peeter Admin | Admin |
| `service@ticketsystem.local` | Service Account | Service |

A new account created on the registration page always gets the role User. The client keeps the session per browser tab, so different roles can be tried in different tabs.

To start from clean data delete `src/TicketSystem.Api/ticketsystem.db` (and the `-shm` and `-wal` files next to it) while the API is stopped.

## API overview

All endpoints except the first three groups require the header `Authorization: Bearer <token>`. The token comes from `POST /api/auth/login` and is valid for 60 minutes.

| Method | Path | Who | Purpose |
|---|---|---|---|
| GET | `/api/categories`, `/api/priorities` | anyone | Reference data |
| POST | `/api/auth/register` | anyone | Create an account (role User) |
| POST | `/api/auth/login` | anyone | Get a JWT token |
| GET | `/api/auth/me` | signed in | Current user |
| POST | `/api/tickets` | signed in | Create a ticket |
| GET | `/api/tickets` | signed in | List with filters, search, sorting and paging (a User sees only own tickets) |
| GET | `/api/tickets/{id}` | author, Support, Admin | One ticket |
| PUT | `/api/tickets/{id}` | author while New, Admin | Edit a ticket |
| POST | `/api/tickets/{id}/assign` | Support | Take a New ticket into work |
| PATCH | `/api/tickets/{id}/status` | assignee, Admin | Change the state |
| GET | `/api/tickets/{id}/log` | author, Support, Admin | Change log |
| DELETE | `/api/tickets/{id}` | Admin | Delete a ticket |
| POST, GET | `/api/tickets/{id}/comments` | author, Support, Admin | Add and read comments |
| GET | `/api/tickets/overdue` | Support, Admin, Service | Unresolved tickets past the deadline |
| PATCH | `/api/tickets/{id}/escalate` | Service | Raise the priority of a late ticket |
| GET | `/api/users/support` | Support, Admin | Support staff for the assignee filter |
| GET | `/api/statistics/resolution-time` | Admin | Average resolution time by category and by support person |
| WebSocket | `/ws/tickets?access_token=<token>` | Support, Admin | Real-time `TicketCreated` and `TicketUpdated` messages |

List parameters of `GET /api/tickets`: `status`, `priority`, `categoryId`, `assignee`, `search`, `sortBy`, `descending`, `page` and `pageSize` (maximum 100).

### Real-time messages

Browsers cannot set headers on a WebSocket, so the token goes in the query string. The server sends JSON messages:

```json
{ "type": "TicketUpdated", "ticket": { "id": 7, "status": "InProgress" } }
```

`ticket` holds the full ticket as in `GET /api/tickets/{id}`. The client reconnects automatically and shows the connection state in the top bar.

## Tests and checks

Unit tests (xUnit and Moq, in-memory SQLite):

```powershell
dotnet test
```

End-to-end check of the API rules. Start the API, then in another window:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-api.ps1
```

The script prints PASS or FAIL for every rule (roles, state changes, deadlines, filters, comments, log, statistics, WebSocket) and creates and removes its own ticket. Use it only with a development database.

### Postman and .http

- `docs/postman/TicketSystem.postman_collection.json`: import it into Postman and run the folders with the Collection Runner in order, starting with `0. Login`. The folders contain a full ticket lifecycle with tests and a set of error and permission checks.
- `src/TicketSystem.Api/TicketSystem.Api.http`: the same endpoints for the REST Client extension of VS Code or Visual Studio.

## Known limitations

- The seeded accounts and the password are for development. The signing key `Jwt:Key` is in `appsettings.Development.json` only for development. The API refuses to start without a key of at least 32 characters, so another environment must set `Jwt:Key` in user secrets or an environment variable.
- The token is passed in the query string of the WebSocket address because browsers cannot send headers there. The task requires this form.
- SQLite `LIKE` ignores case only for the letters A-Z, so the search is case-sensitive for letters such as õ, ä, ö and ü.
- Two support people pressing "take into work" at the same moment on the same ticket is not protected by a concurrency token. The second request may overwrite the first.
- Escalation raises the priority but keeps the original deadline.
- Users get no push messages. They see changes after a reload, when the tab gets focus again or with the refresh button. The WebSocket is for support staff and administrators, as in the task.
- The client has no automated tests. It is checked with `npm run lint`, `npm run build` and by hand.
- `dotnet build` shows warning NU1903 about `SQLitePCLRaw.lib.e_sqlite3`. No fixed version is available yet.
