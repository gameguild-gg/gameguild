# Deployment Smoke Checks

Date: 2026-06-19

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

The script verifies:

- API `/live`, `/health`, and documentation.
- Web app health, root, courses, programs, and learning dashboard routes.
- Learning app root and sign-in route.

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
