# Authorization cache and invalidation strategy

**Module:** `GameGuild.Identity.Authorization`
**Last updated:** October 2026
**Criticality:** Security relevant

## Current implementation

Authorization caching uses `HybridPermissionCache`: a process-local L1 `IMemoryCache` and an optional Redis-backed L2 `IDistributedCache`. The L2 layer is enabled with `AddAuthorizationRedisCache`; the authorization module can also run with L1 only.

ACL cache keys include tenant, user, and shared tenant/global security versions. The tenant and reserved global versions are read together from the database-backed `DatabaseTenantSecurityVersionStore`. Policy cache entries are versioned by tenant. This means a warm ACL lookup still reads the authoritative security versions before selecting a cached decision; the cache does not trade away revocation correctness for an unverified local version.

Permission changes use `ICacheInvalidationService`. Tenant, user, resource, policy, batch, and global invalidations advance the applicable shared version before local eviction and Redis Pub/Sub publication. Versioned keys make old decisions unreachable if publication fails; Pub/Sub gives other instances prompt L1 cleanup. The Redis subscriber validates incoming events and retries after subscription failures. Batch invalidation accepts up to 500 typed user, resource, policy, or role/group dependency targets.

Redis L2 read failures or invalid serialized values are treated as cache misses so the calling service can query its authoritative store. L2 write and removal failures are logged without failing the request. Cancellation requested by the caller still propagates. Database/version-store failures are not converted into cache hits.

## Capacity, expiration, and warming

Configure `AuthorizationCacheOptions` in `apps/api/Source/Modules/GameGuild.SharedKernel/Configuration/PresentationLayer/Authorization/AuthorizationCacheOptions.cs`. It provides TTLs for policies, permissions, ACLs, and rulesets; an L1 tracked-entry bound; an L2 TTL; Redis/Pub/Sub options; and automatic warming limits. The tracked-entry cap applies to authorization keys managed by `PermissionCacheKeyTracker`; it does not set a size limit on the application's shared `IMemoryCache`.

`PermissionCacheWarmupService` supports explicit bounded warming through the system-admin endpoint. `AutomaticPermissionCacheWarmupService` maintains a bounded, process-local popularity window and periodically preloads frequently accessed subject/resource pairs through the ordinary versioned ACL path. Automatic warming can be disabled in configuration.

## Administration and observability

The hidden permission administration controller exposes:

- `GET /v{version}/permissions/cache/stats` for system administrators;
- `POST /v{version}/permissions/cache:clear` for system administrators or an administrator of the targeted tenant;
- `POST /v{version}/permissions/cache:warm` for system administrators.

Cache counters and lookup-duration instruments use the `GameGuild.Identity.Authorization.Cache` meter. When OpenTelemetry is enabled, API configuration exports the meter through the selected exporter. The statistics endpoint reports process-local L1 entry counts and counters; use the configured telemetry collector for aggregation across replicas and percentile analysis. Example Prometheus rules live in [`docs/api/authorization-cache-alerts.yml`](../api/authorization-cache-alerts.yml). They are templates: thresholds and alert delivery still require validation in the deployment's telemetry and notification setup.

The detailed setup, failure behavior, and endpoint guidance are in [`docs/api/authorization-cache-configuration.md`](../api/authorization-cache-configuration.md). Comparative local benchmark results and their limits are in [`apps/api/tests/GameGuild.Identity.Authorization.PerformanceTests/README.md`](../../apps/api/tests/GameGuild.Identity.Authorization.PerformanceTests/README.md). Current local fixtures do not establish a consistent latency improvement or production capacity; repeat measurements with representative data and deployment topology before making those claims.
