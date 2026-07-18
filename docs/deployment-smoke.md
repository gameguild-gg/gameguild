# Deployment Smoke Checks

Date: 2026-07-17

Run the smoke gate after starting local services or after a Coolify redeploy:

```bash
pnpm smoke
```

For the public staging domains:

```bash
pnpm smoke:live
```

Coolify is configured to deploy this branch from the Git push webhook. A normal release must use exactly one trigger:

1. Push the verified commit and wait for the automatic Coolify deployment.
2. Use the manual Coolify deploy API only for recovery when no webhook deployment exists.

Never trigger the manual deploy API immediately after a push; that creates two deployments for the same commit and can replace a healthy container while the live smoke is running.

The API checks verify:

- Liveness on `/live` and deployment readiness on `/ready`.
- The Swagger document is reachable and exposes exactly these five Orders operations:
  - `POST /v1/orders`
  - `GET /v1/orders/{orderId}`
  - `POST /v1/orders/{orderId}/items`
  - `POST /v1/orders/{orderId}:capture`
  - `POST /v1/orders/{orderId}:complete`
- API documentation and administrator authentication.

The remaining checks verify:

- Web app health, root, courses, programs, and learning dashboard routes.
- Learning app root and sign-in route.

## Coolify startup configuration

Set `POSTGRES_MIGRATION_CONNECTION` to a full PostgreSQL connection string for a DDL-capable migration role. Its username must differ from `POSTGRES_USER`; the API fails startup initialization when the migration connection is absent or resolves to the runtime role. Do not grant blanket table privileges to the runtime role after migrations.

Set the canonical ASP.NET Core Stripe variables in Coolify:

```dotenv
PaymentGateways__Stripe__ApiKey=...
PaymentGateways__Stripe__PublishableKey=...
Billing__Stripe__WebhookSecret=...
Billing__Stripe__WebhookEndpointId=...
Billing__Stripe__ApiVersion=...
Billing__Stripe__ConnectedAccountId=
Billing__Stripe__LiveMode=false
Billing__Stripe__WebhookToleranceSeconds=300
```

Compose pins `PaymentGateways__Stripe__IsEnabled=true` and `PaymentGateways__Stripe__UseSimulation=false`. Use Stripe test-mode keys and `Billing__Stripe__LiveMode=false` in Staging. Production requires live-mode keys and `Billing__Stripe__LiveMode=true`.

After the infrastructure smoke passes, run the authenticated product journeys:

```bash
API_BASE_URL=https://game-guild-api.example.com \
PROFESSOR_E2E_BASE_URL=https://game-guild.example.com \
pnpm --filter @game-guild/web test:browser:learning-professor

API_BASE_URL=https://game-guild-api.example.com \
COMMUNITY_ADMIN_E2E_BASE_URL=https://game-guild.example.com \
pnpm --filter @game-guild/web test:browser:community-admin
```

Both journeys create unique fixtures, verify persistence through the API, and remove or archive their temporary state before exiting.

Default local URLs:

| App | URL |
| --- | --- |
| API | `http://localhost:5296` |
| Web | `http://localhost:3005` |
| Learning | `http://localhost:3006` |

For a deployed environment other than the default staging domains, override the defaults with:

```bash
GAMEGUILD_API_URL=https://game-guild-api.example.com \
GAMEGUILD_WEB_URL=https://game-guild.example.com \
GAMEGUILD_LEARNING_URL=https://game-guild-learning.example.com \
pnpm smoke
```
