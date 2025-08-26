# Environment & Development Setup

Consolidated summary.

## Environment Variables (example)

```env
DB_CONNECTION_STRING=Host=localhost;Database=gameguild;Username=postgres;Password=postgres
ASPNETCORE_ENVIRONMENT=Development
NEXT_PUBLIC_API_URL=http://localhost:5000
```

## Database

1. Install EF tooling: `dotnet tool install --global dotnet-ef`
2. Apply migrations (from `apps/api`): `dotnet ef database update`
3. Seeding creates initial permission scaffolding if DB empty

## Running Services

- API: `dotnet run --project apps/api/GameGuild.csproj`
- Web: (in `apps/web`) `npm run dev`

## Health Check

`GET /health` (public) verifies API availability.

## Common Issues

| Symptom | Cause | Fix |
|---------|-------|-----|
| 401 protected endpoint | Missing / expired JWT | Complete sign-in; ensure refresh working |
| ECONNREFUSED from web  | API not running        | Start API on port 5000 |
| Migration errors       | Schema drift           | Drop local DB & re-run migrations |

## Refresh Tokens & Session

Frontend refreshes access token ~30s pre-expiry; backend validates & rotates pair (see `auth-module.md`).

## Git Conventions

Follow Conventional Commits (see root guidelines).
