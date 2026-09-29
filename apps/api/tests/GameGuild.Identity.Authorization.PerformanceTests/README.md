# Authorization cache benchmarks

This project contains two complementary benchmarks:

- `PermissionCacheLookupBenchmarks` compares a direct in-memory permission lookup with a warm L1 cache lookup.
- `PermissionCacheDatabaseLookupBenchmarks` compares database-backed and warm cached ACL batches at concurrency levels 1, 8, and 32.

The database benchmark starts a disposable PostgreSQL 16 container, creates indexed ACL and tenant-version tables, and seeds 5,000 unrelated ACL entries plus the grant under test. It exercises `DatabaseAccessControlListService` and `CachedAccessControlListService`; the ACL repository adapter runs a PostgreSQL query with the same tenant, resource, active, expiry, and principal filters as `AccessControlListEntryRepository`. The cached case includes the production `DatabaseTenantSecurityVersionStore` lookup of tenant and global versions, then measures a warm per-process L1 ACL lookup. Each database access uses a pooled connection, so independent lookups can execute concurrently. The fixture omits Entity Framework materialization, Redis, production data distribution, and networked application deployment. Treat it as a local comparative fixture, not a production capacity result.

## Local comparison (2026-09-29)

ShortRun used 10 warmup iterations and 10 measured iterations on Windows 10, .NET 10.0.10, with Docker PostgreSQL 16. The values are total time for each concurrent batch, not per-request latency:

| Concurrent lookups per batch | Database ACL batch | Warm cached ACL batch | Cache/database ratio |
| ---: | ---: | ---: | ---: |
| 1 | 763.6 µs | 673.6 µs | 0.89 |
| 8 | 2.312 ms | 3.218 ms | 1.43 |
| 32 | 9.068 ms | 7.031 ms | 0.82 |

The 99.9% confidence intervals overlap widely at every concurrency level. This run does not establish a consistent performance improvement; repeat with production-like EF queries, deployment resources, and data before accepting the performance criterion.

Run the database benchmark with Docker available:

```powershell
dotnet run --project apps/api/tests/GameGuild.Identity.Authorization.PerformanceTests/GameGuild.Identity.Authorization.PerformanceTests.csproj --configuration Release -- --filter "*PermissionCacheDatabaseLookupBenchmarks*" --iterationCount 10 --warmupCount 10 --launchCount 1 --artifacts "$env:TEMP\GameGuild.AuthorizationCacheBenchmarks"
```

```bash
dotnet run --project apps/api/tests/GameGuild.Identity.Authorization.PerformanceTests/GameGuild.Identity.Authorization.PerformanceTests.csproj --configuration Release -- --filter '*PermissionCacheDatabaseLookupBenchmarks*' --iterationCount 10 --warmupCount 10 --launchCount 1 --artifacts "${TMPDIR:-/tmp}/gameguild-authorization-cache-benchmarks"
```

Benchmark results describe this isolated local fixture. Use a production-like database, data distribution, deployment topology, and repeated runs before using them for capacity planning.
