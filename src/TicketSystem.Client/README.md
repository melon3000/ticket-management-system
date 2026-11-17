# Ticket system client

React (Vite, TypeScript) client of the ticket system. It talks to the Web API in `../TicketSystem.Api`.

## Run

```
npm install
npm run dev
```

The client opens at http://localhost:5173. The API must run at http://localhost:5119 (the address is allowed by the CORS setting `Cors:AllowedOrigins` of the API).
To use another API address, set `VITE_API_URL` before starting, for example `VITE_API_URL=http://localhost:5200 npm run dev`.

## Scripts

| Command | What it does |
| --- | --- |
| `npm run dev` | Starts the development server |
| `npm run build` | Type-checks and builds the client into `dist/` |
| `npm run lint` | Checks the code with ESLint |

## Structure

- `src/api` - requests to the Web API and the types of its answers
- `src/auth` - sign-in state, the saved session and the roles
- `src/router` - a small router based on the part of the address after `#`
- `src/components` - parts that several pages share
- `src/pages` - one file per page
