# Deployment Smoke Checks

Date: 2026-06-19

Run the smoke gate after starting local services or after a Coolify redeploy:

```bash
pnpm smoke
```

The script verifies:

- API `/live`, `/health`, and documentation.
- Web app health, root, courses, programs, and learning dashboard routes.
- Learning app root and sign-in route.

Default local URLs:

| App | URL |
| --- | --- |
| API | `http://localhost:5296` |
| Web | `http://localhost:3005` |
| Learning | `http://localhost:3006` |

For a deployed environment, override the defaults with:

```bash
GAMEGUILD_API_URL=https://game-guild-api.example.com \
GAMEGUILD_WEB_URL=https://game-guild.example.com \
GAMEGUILD_LEARNING_URL=https://game-guild-learning.example.com \
pnpm smoke
```
