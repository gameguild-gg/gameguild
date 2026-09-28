# Authorization cache configuration

Authorization caching uses an in-memory L1 cache and an optional Redis L2 cache.
Register the Redis helper before the authorization cache so the same Redis
connection and invalidation channel are available to the cache and its
subscriber:

```csharp
var redisConnectionString = configuration.GetConnectionString("AuthorizationRedis")
    ?? throw new InvalidOperationException("AuthorizationRedis connection is required.");

services.AddAuthorizationRedisCache(redisConnectionString, instanceName: "gg:auth:");
services.AddAuthorizationCaching(options =>
{
    options.PolicyTtlSeconds = 300;
    options.PermissionTtlSeconds = 300;
    options.AccessControlListTtlSeconds = 60;
    options.DistributedCacheTtlSeconds = 600;
    options.MaxL1CacheSize = 5000;
    options.UsePubSubInvalidation = true;
    options.InvalidationChannelName = "gg:auth:invalidate";
});
```

Keep the Redis connection string in the deployment secret store. `AddAuthorizationRedisCache`
configures `IDistributedCache`, enables L2, registers the StackExchange.Redis
multiplexer used for Pub/Sub, and starts the per-instance invalidation subscriber.
The subscriber retries subscription failures; StackExchange.Redis restores an
active subscription after reconnecting. See the official [connection behavior
documentation](https://github.com/StackExchange/StackExchange.Redis/blob/main/docs/Configuration.md)
and [Pub/Sub reconnect fixes](https://github.com/StackExchange/StackExchange.Redis/blob/main/docs/ReleaseNotes.md).

The cache options also include `RulesetTtlSeconds`, `MaxPolicyCacheSize`,
`EnableMetrics`, and `MetricsLoggingIntervalSeconds`. The Redis channel must be
non-empty when distributed Pub/Sub invalidation is enabled. Use a separate
channel for each isolated application deployment that shares the same Redis
server.

`MaxL1CacheSize` bounds the entries registered through the authorization cache
key tracker. When the cap is exceeded, the tracker removes a prior L1 entry and
records a `capacity` eviction. This cap is scoped to tracked authorization
entries; it does not set `SizeLimit` on the shared application `IMemoryCache`.

Permission changes advance the shared tenant security version before publishing
user, resource, or policy invalidation events. Cache keys include this version,
so entries from the old version are no longer selected if a Pub/Sub publish
fails. Publish failures are logged, and old cache entries expire through their
configured TTLs. Pub/Sub provides prompt local cleanup. A singleton per-process key index tracks L1 entries from cache services and request scopes so received events can evict the actual local entries.
Wildcard matching scans this in-memory index; the implementation does not issue Redis key scans for deletion.

ACL keys also include a shared global security version stored under the reserved
`Guid.Empty` version scope. Global role assignment, removal, update, and deletion
advance this version and publish a global invalidation event. This makes prior
ACL keys unreachable across tenants even when Pub/Sub is unavailable; received
events also clear tracked ACL entries from the local L1 cache.
The tenant and global versions are read together in one database query.

Bulk invalidation accepts 1–500 typed targets (user, resource, policy, or role/group
dependency), advances the tenant version once, evicts matching local L1 entries,
and publishes one versioned Redis event. ACL grant and revoke operations publish
the affected resource together with its role or group dependency, so cached
subject decisions are evicted across request scopes. The subscriber validates
batch targets before applying them. This covers direct ACL dependencies; broader
hierarchical inheritance invalidation still needs explicit verification.

The hidden permission administration controller exposes
`GET /v{version}/permissions/cache/stats` to system administrators and
`POST /v{version}/permissions/cache:clear` for the authorized user or tenant
scope. The statistics response reports process-local L1 entry counts, distinct
users represented by those keys, and the recorded L1/L2 hits, misses, evictions,
and per-cache-type counts. It does not aggregate across instances. The legacy
latency field in this statistics response remains empty. OpenTelemetry exports
`authorization_cache_lookup_duration` as a millisecond histogram tagged by
`cache_type` for L1/L2/miss cache lookup time. It measures the cache layer only;
it does not include the authoritative database lookup a caller performs after a
miss.

System administrators can prewarm up to 500 selected ACL decisions with
`POST /v{version}/permissions/cache:warm`. Each item names a tenant resource and
either a user with role/group IDs or the anonymous subject. The endpoint uses
the normal versioned ACL evaluation path and returns only requested, warmed, and
duplicate counts; it does not return access decisions. This is explicit
operator-selected prewarming. Automatic popularity discovery and scheduled
predictive warming are not implemented.

If an L2 read fails or contains invalid data, the hybrid cache records a warning
and returns a miss so the calling service can load the authoritative value from
its backing store. L2 write and removal failures are logged without failing the
request; the L1 operation remains in effect. Caller-requested cancellation still
propagates.

The authorization and authentication unit suites cover the L1/L2 cache behavior,
DI registration, version increments, serialized event payloads, subscriber
dispatch and retry, Redis publish failure handling, and cache availability when
L2 reads, writes, or removals fail. The Redis integration project contains
focused cases for stale-ACL protection, Pub/Sub dispatch, role/group dependency
invalidation, and subscriber recovery after a Redis restart. Those integration
cases require Docker and were not executable in environments without a running
Docker daemon.

Full network fault injection during Redis reconnects, inherited hierarchy
invalidation, automatic popularity-based cache warming, cross-instance metric
aggregation, production-representative performance/load measurements, and
operational alert thresholds remain open work.
