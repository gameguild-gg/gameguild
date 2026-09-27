# Bulk permission checks

`IPermissionService` exposes two bulk read patterns:

- `BulkCheckPermissionsAsync(userIds, tenantId, permissions)` evaluates the requested permission set for each user in one tenant and returns a materialized matrix. It is suitable when the caller needs the full matrix at once.
- `StreamBulkCheckPermissionsAsync(requests, batchSize, cancellationToken)` evaluates individual user, tenant, content-type, and resource decisions in bounded batches. It preserves input order and duplicate requests and yields each result before reading the next batch.

Each streamed request contains one user, tenant, and permission. `ContentTypeName` and `ResourceId` add narrower scopes. A resource check must include `ResourceTypeName`; this prevents a resource ID reused by another entity type from granting access. Global defaults apply to all tenants, tenant defaults apply only to that tenant, and user grants apply only to that user and tenant. Tenant-level deny entries override matching allows. Content-type and resource grants add permissions at their respective scopes.

The streaming batch size defaults to 128 and is limited to 256 so query parameter sets and intermediate result collections remain bounded. Use the streaming API for large inputs; the collection overload deliberately materializes all results. Cancellation is passed to each database query and honored while reading the input stream.

```csharp
await foreach (var decision in permissionService.StreamBulkCheckPermissionsAsync(
    permissionChecks,
    batchSize: 128,
    cancellationToken))
{
    if (decision.IsGranted)
    {
        // Process the authorized item without holding the full result set.
    }
}
```

The bulk APIs query grants by the distinct users, tenants, content types, and resource IDs present in each batch, then evaluate the exact combinations in memory. Repeated identical decisions are memoized within that batch, but decisions are not cached across calls; permission changes therefore cannot leave stale cross-request results. Production database benchmarks and load tests should use the configured PostgreSQL provider and representative tenant/grant distributions. The in-memory benchmark/test fixtures are useful for comparing algorithm shape, not for setting production latency targets.

Run the comparative in-memory benchmark with:

```powershell
dotnet run --project apps/api/tests/GameGuild.Identity.Authentication.Benchmarks -c Release -- --filter *PermissionBulkCheckBenchmarks* --job short
```

It compares individual checks, the existing user/permission matrix, and streamed mixed-context checks at 100 and 1,000 users. Use PostgreSQL-backed integration/load measurements before setting production performance claims.

One `Dry` run on the local Windows 10 / .NET 10.0.10 / EF InMemory environment measured:

| Users | Individual checks | Bulk matrix | Streamed mixed-context |
| ---: | ---: | ---: | ---: |
| 100 | 255.2 ms | 174.1 ms | 181.6 ms |
| 1,000 | 3.866 s | 181.2 ms | 239.5 ms |

This is one cold-start sample per case, with no confidence interval. It demonstrates the query-count trend in the in-memory fixture only; it is not a PostgreSQL latency benchmark or a release threshold.
