# Production social feed release notes

Date: 2026-09-10

## Database and rollout

The `20260910042305_AddProductionSocialFeed` migration is additive. It creates `social_saved_posts`, `social_stories`, and `social_story_views`, plus the active repost uniqueness index. Existing posts, profiles, and user memberships are not rewritten. Apply it with the normal deployment migration role configured through `POSTGRES_MIGRATION_CONNECTION`; the runtime database role must remain separate and non-DDL.

The API and Web deployment must use the same generated social contract. Assets must have working S3-compatible storage and delivery configuration (`S3_BUCKET`, `S3_CONTAINER_SERVICE_URL`, `S3_ACCESS_KEY`, `S3_SECRET_KEY`, and `S3_REGION`, or the corresponding `Assets__Storage__*` settings). The browser verification uploads real image media and rejects a run when storage processing or delivery is unavailable.

## Non-admin release evidence

Provision two existing users in the same tenant. Both access tokens must carry an explicit non-administrator role; `SystemAdmin`, `TenantAdmin`, `Admin`, and `Owner` are rejected. The runner never signs users up and has no committed password fallback.

Configure these values outside source control:

```dotenv
SOCIAL_FEED_E2E_USER_A_EMAIL=...
SOCIAL_FEED_E2E_USER_A_PASSWORD=...
SOCIAL_FEED_E2E_USER_B_EMAIL=...
SOCIAL_FEED_E2E_USER_B_PASSWORD=...
GAMEGUILD_SMOKE_ADMIN_EMAIL=...
GAMEGUILD_SMOKE_ADMIN_PASSWORD=...
```

Run the browser journey before the deployment smoke:

```bash
API_BASE_URL=https://api.example.test \
SOCIAL_FEED_E2E_BASE_URL=https://web.example.test \
pnpm --filter @game-guild/web test:browser:social-feed

GAMEGUILD_API_URL=https://api.example.test \
GAMEGUILD_WEB_URL=https://web.example.test \
pnpm smoke
```

Use identical normalized API and Web origins in both commands; the smoke intentionally rejects evidence generated against a different target.

The browser journey writes `.tmp/social-feed-browser-e2e/evidence.json` by default. Override it with `SOCIAL_FEED_E2E_EVIDENCE_PATH` when the browser and smoke execute in different workspaces. The artifact is ignored by Git, contains capability booleans, aggregate status, errors, target origins, timing, and actor IDs, and never contains passwords or tokens. The smoke accepts only a passing artifact for the same API/Web origins that is no more than 24 hours old by default; override that window with `SMOKE_SOCIAL_EVIDENCE_MAX_AGE_MS` only under an explicit release policy. The override must be a finite non-negative integer in milliseconds or the smoke fails closed.

The smoke validates the social OpenAPI surface and consumes the browser artifact. It does not claim to execute the two-user journey itself.

## Rollback

Roll back the application binaries first so no running instance writes the new social tables. The migration `Down` path then drops only the three added tables and the active repost uniqueness index. Saved posts, stories, and story views created after rollout are lost when that database rollback is applied, so retain a database backup when that data must be recoverable. Object-storage media is managed by the Assets lifecycle and is not deleted by the database migration rollback.
