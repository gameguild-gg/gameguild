# Devtron production release runbook

Devtron is the production source of truth for GameGuild. The live Devtron applications read `main` from the private Forgejo mirror `gameguild-gg/gameguild`; that mirror follows GitHub and is the Git material configured in Devtron for API and Web. Both production CD pipelines use `AUTOMATIC` triggers from their internal Forgejo CI pipelines.

The immutable external-CI flow later in this document is a target contract only. GitHub does not push PR candidates to the public registry and does not automatically run `Production Release` on `main` while Devtron remains on internal CI. This avoids making the required PR gate depend on Cloudflare's upload limits or on release credentials that are not part of the live topology.

The service-level objective is p95 from an approved merge to a verified healthy production release in 10 minutes or less.

## Current live release path

1. `PR Verify` classifies the diff and runs only the required Web, API, Testing Lab, OpenAPI, migration, or Economy gates.
2. `PR Required Gate` aggregates every selected verification job and is the required status check on `main`.
3. The approved change is merged to GitHub `main` and the private Forgejo mirror is synchronized.
4. Devtron's internal CI pipelines build API and Web from Forgejo `main` and publish to the in-cluster registry path.
5. The automatic production CD pipelines deploy the resulting service images.
6. Operators verify Kubernetes rollout state, workload health, public health endpoints, and the affected Learning and Testing Lab routes.

Normal production releases must be monitored in Devtron and Kubernetes until both workloads are ready. The GitHub `Production Release` workflow is manual-only and must not be used until the external-CI contract below is fully configured and proven in staging.

## Target immutable external-CI path

The remaining sections specify the future flow in which PR jobs build immutable candidates, GitHub promotes exact digests, and Devtron accepts external-CI webhooks. They are retained as the migration target, not as a description of the current deployment.

## GitHub production environment

Create the `production` environment without a wait timer or manual approval for normal releases. Limit environment administration and secret changes to production administrators. The workflow itself restricts manual hotfix promotion to repository administrators.

Required variables:

| Variable                              | Contract                                                                             |
| ------------------------------------- | ------------------------------------------------------------------------------------ |
| `DEVTRON_REGISTRY_HOST`               | Registry hostname used by both GitHub and Devtron                                    |
| `DEVTRON_REGISTRY_NAMESPACE`          | Namespace containing `gameguild-api`, `gameguild-web`, and `gameguild-release-state` |
| `DEVTRON_BASE_URL`                    | Devtron origin, without a trailing path                                              |
| `DEVTRON_EXTERNAL_CI_ID_API`          | API external-CI pipeline identifier                                                  |
| `DEVTRON_EXTERNAL_CI_ID_WEB`          | Web external-CI pipeline identifier                                                  |
| `DEVTRON_RELEASE_IDENTITY_CONFIGURED` | Must be `true` only after the runtime identity contract below is verified            |
| `DEVTRON_API_PREDEPLOY_MIGRATIONS`    | Must be `true` only after the API migration pre-deploy job is configured             |
| `GAMEGUILD_API_URL`                   | `https://api.gameguild.gg` in production                                             |
| `GAMEGUILD_WEB_URL`                   | `https://gameguild.gg` in production                                                 |
| `GAMEGUILD_SMOKE_PROJECT_ID`          | Stable tenant project readable by the smoke administrator                            |
| `CLOUDFLARE_ZONE_ID`                  | Zone purged only after the rollout and smoke pass                                    |

The candidate build also uses `GAMEGUILD_API_INTERNAL_URL`, `GAMEGUILD_API_PUBLIC_URL`, `GAMEGUILD_WEB_PUBLIC_URL`, `GAMEGUILD_AUTH_COOKIE_DOMAIN`, and `GAMEGUILD_GOOGLE_CLIENT_ID`.

Required secrets:

- `DEVTRON_REGISTRY_USERNAME` and `DEVTRON_REGISTRY_PASSWORD`
- `DEVTRON_API_TOKEN`
- `GAMEGUILD_SMOKE_ADMIN_EMAIL` and `GAMEGUILD_SMOKE_ADMIN_PASSWORD`
- `CLOUDFLARE_API_TOKEN`

Never set `ALLOW_INITIAL_RELEASE_WITHOUT_STABLE=true` in production. It is a staging bootstrap switch only.

## Devtron application contract

Create one external-CI pipeline per service. The webhook endpoint is `/orchestrator/webhook/ext-ci/<externalCiId>` and receives `dockerImage`, `digest`, and `ciProjectDetails`. Disable Git-triggered builds and automatic rebuilds. The incoming image digest is the deployment artifact.

Both workloads must use:

- rolling update with `maxUnavailable: 0` and `maxSurge: 1`;
- `progressDeadlineSeconds: 300`;
- readiness before traffic and existing liveness probes;
- no mutable `latest` image references;
- enough termination grace for in-flight requests.

API and Web must receive the following runtime values for each external-CI deployment:

| Environment variable | Source                                                        |
| -------------------- | ------------------------------------------------------------- |
| `RELEASE_SHA`        | `ciProjectDetails[0].commitHash` from the external-CI payload |
| `SOURCE_TREE`        | OCI image label/build value baked into the candidate          |
| `IMAGE_DIGEST`       | Incoming immutable `digest`                                   |
| `VERSION`            | OCI image label/build value baked into the candidate          |
| `BUILD_TIMESTAMP`    | OCI image label/build value baked into the candidate          |
| `DEPLOYED_AT`        | UTC timestamp generated by the deployment                     |

Do not set `DEVTRON_RELEASE_IDENTITY_CONFIGURED=true` until a staging rollout proves that Web `/api/health` and API `/health` return the exact release SHA, tree, and digest plus non-`Unknown` `version`, `builtAt`, and `deployedAt`. Both endpoints must emit `X-GameGuild-Release-Sha`.

## Migrations

The API pipeline owns a pre-deploy migration job using the migration role. It runs before API rollout and blocks the deployment on failure. Migrations must follow expand/contract: deploy additive schema first, move readers and writers, then remove obsolete schema in a later release. Image rollback never attempts to reverse database schema.

Only after that job is proven in staging may `DEVTRON_API_PREDEPLOY_MIGRATIONS=true` be set.

## Stable state, retention, and cutover

The release manifest is stored as an OCI artifact in `gameguild-release-state:release-<releaseSha>` and copied to `gameguild-release-state:stable` only after smoke succeeds. Seed staging once with `ALLOW_INITIAL_RELEASE_WITHOUT_STABLE=true`; after the first healthy state exists, remove the switch and test rollback before production cutover.

Configure the registry retention policy to preserve at least the newest 20 release tags for every service and all release-state manifests for 30 days. Candidate tags may be garbage-collected after their corresponding release manifest is retained.

Cutover requires three consecutive staging releases that demonstrate:

- Web-only changes deploy only Web;
- API and Web changes deploy API first;
- migration failure stops before rollout;
- identity matches the manifest;
- a forced smoke failure restores previous digests;
- each merge reaches healthy state within 10 minutes.

Production acceptance requires the same five invariants for three consecutive releases. Record `merge timestamp`, `healthy timestamp`, `releaseSha`, affected services, workflow run, and rollback result. Report p50, p95, and maximum lead time.

## Main protection

After this workflow has produced `PR Required Gate` on `main`, activate the versioned ruleset:

```bash
APPLY_MAIN_RULESET=true bash scripts/deploy/apply-main-ruleset.sh
```

The script creates or updates `main-production`, requires one approval, the latest `main`, resolved conversations, squash merge, and `PR Required Gate`. It also disables merge commits and rebase merges repository-wide. When the external-CI migration is activated, `Production Release` must independently require a successful PR run and matching candidate evidence. Until then, normal releases follow the current live path above and repository policy must enforce the PR gate.

## Audited hotfix

1. Branch `hotfix/<incident>` from current `main`.
2. Open an internal PR targeting `main`; do not bypass `PR Verify`.
3. Keep the branch current with `main`. Economy-critical changes still run the complete Economy gate.
4. A production administrator manually runs `Production Release` with the branch, incident/change record, and exact confirmation `DEPLOY HOTFIX`.
5. The workflow promotes only candidates from the successful PR run and records actor, branch, PR, incident, and source SHA in the run summary.
6. Merge the hotfix PR by squash after stabilization and forward-port it to `develop` when applicable.

## Incident response

If a rollout exceeds five minutes, health identity differs, or smoke fails, the workflow automatically rolls affected services back in reverse order. If automatic rollback fails, use the `previous-release-manifest.json` artifact from the failed run to submit each prior `image@digest` to the corresponding Devtron external-CI pipeline. Do not rebuild, retag a different digest, or reverse a database migration.

Cloudflare purge must remain after verified smoke. A failed release must not update `gameguild-release-state:stable`.

## Legacy Coolify/Compose window

Coolify is not a production deployment authority. Keep the existing Compose/Coolify recovery material read-only for the first three successful Devtron production releases. After those releases, remove Coolify credentials and webhook access and remove Compose from the production procedure; Compose remains available for local development only.
