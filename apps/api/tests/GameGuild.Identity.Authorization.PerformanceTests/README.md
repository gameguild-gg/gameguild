# Authorization cache benchmarks

This project contains three complementary benchmarks:

- `PermissionCacheLookupBenchmarks` compares a direct in-memory permission lookup with a warm L1 cache lookup.
- `PermissionCacheDatabaseLookupBenchmarks` compares database-backed and warm cached ACL batches at concurrency levels 1, 8, and 32.
- `PermissionCacheEfDatabaseLookupBenchmarks` compares the current ACL repository through EF Core/Npgsql against a warm L1 cache while retaining the database-backed tenant/global security-version lookup.

The raw-SQL database benchmark starts a disposable PostgreSQL 16 container, creates indexed ACL and tenant-version tables, and seeds 5,000 unrelated ACL entries plus the grant under test. It exercises `DatabaseAccessControlListService` and `CachedAccessControlListService`; the ACL repository adapter runs a PostgreSQL query with the same tenant, resource, active, expiry, and principal filters as `AccessControlListEntryRepository`. The cached case includes the production `DatabaseTenantSecurityVersionStore` lookup of tenant and global versions, then measures a warm per-process L1 ACL lookup. The shared Testcontainers helper disables pooling for ordinary test isolation, so this benchmark explicitly enables Npgsql pooling with a maximum of 128 connections. This lets independent lookups execute concurrently without measuring repeated TCP connection setup. The fixture omits EF materialization, Redis, production data distribution, and networked application deployment. Treat it as a local comparative fixture, not a production capacity result.

## Raw-SQL local comparison (2026-10-03)

ShortRun used 8 warmup iterations, 10 measured iterations, and one launch on Windows 10, .NET 10.0.10, with Docker PostgreSQL 16. Values are total time for each concurrent batch, not per-request latency. Pooling is enabled explicitly for this run. These results replace the 2026-09-29 values, which inherited `Pooling=false` from the shared test database helper and therefore did not model pooled API connections.

| Concurrent lookups per batch | Database ACL batch | Warm cached ACL batch | Cache/database ratio | Managed allocation ratio |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 0.767 ms ± 0.092 ms | 0.569 ms ± 0.029 ms | 0.75 | 0.53 |
| 8 | 1.916 ms ± 0.101 ms | 1.472 ms ± 0.211 ms | 0.77 | 0.51 |
| 32 | 6.027 ms ± 0.846 ms | 4.705 ms ± 0.870 ms | 0.79 | 0.51 |

The reported error intervals are 99.9% confidence intervals. They overlap at concurrency 32, and this one-launch raw-SQL comparison is diagnostic only. The EF-backed comparison below uses three launches and is the stronger local measurement; neither fixture establishes production capacity.

## EF-backed local comparison (2026-10-03)

The EF benchmark uses the current `AccessControlListEntryRepository` and `TenantSecurityVersionRepository` with a request-scoped test `DbContext` per lookup. It starts PostgreSQL 16 in Docker, seeds 5,000 unrelated ACL rows plus the grant under test, and compares database ACL reads with a warm process-local L1 ACL cache. Both paths read the tenant/global security versions from PostgreSQL. The shared Testcontainers helper disables Npgsql pooling for ordinary test isolation, so this benchmark explicitly turns pooling on with a maximum of 128 connections; this models API request handling and avoids measuring repeated TCP connection setup. The fixture does not run the full API `ApplicationDbContext`, Redis, production data distribution, or a networked application deployment.

BenchmarkDotNet 0.14.0 ran 10 warmup iterations, 20 measured iterations, and 3 launches on Windows 10 with .NET 10.0.10. Values are total time and managed allocation for a concurrent batch. The reported error intervals are 99.9% confidence intervals. The cache/database ratio is below 1 at all three concurrency levels; the measured batch latency is about 23–26% lower for the cached path in this fixture.

| Concurrent lookups per batch | PostgreSQL + EF batch | Warm L1 + EF version-read batch | Cache/database latency ratio | Managed allocation ratio |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 1.093 ms ± 0.061 ms | 0.815 ms ± 0.038 ms | 0.76 | 0.77 |
| 8 | 3.156 ms ± 0.109 ms | 2.330 ms ± 0.079 ms | 0.74 | 0.77 |
| 32 | 11.258 ms ± 0.594 ms | 8.721 ms ± 0.430 ms | 0.78 | 0.77 |

The cache/database latency intervals do not overlap at the three measured concurrency levels. The cache also reduced managed allocation by about 23% per batch. This is evidence for the isolated local fixture only; it does not establish production capacity or behavior with Redis, representative production data, the full API context, or networked deployment. Repeat against the configured deployment topology before treating the production-performance criterion as verified.

Run the database benchmark with Docker available:

```powershell
dotnet run --project apps/api/tests/GameGuild.Identity.Authorization.PerformanceTests/GameGuild.Identity.Authorization.PerformanceTests.csproj --configuration Release -- --filter "*PermissionCacheDatabaseLookupBenchmarks*" --iterationCount 10 --warmupCount 8 --launchCount 1 --artifacts "$env:TEMP\GameGuild.AuthorizationCacheBenchmarks"
```

Run the EF-backed benchmark with Docker available:

```powershell
dotnet run --project apps/api/tests/GameGuild.Identity.Authorization.PerformanceTests/GameGuild.Identity.Authorization.PerformanceTests.csproj --configuration Release -- --filter "*PermissionCacheEfDatabaseLookupBenchmarks*" --iterationCount 20 --warmupCount 10 --launchCount 3 --artifacts "$env:TEMP\GameGuild.AuthorizationEfCacheBenchmarks"
```

```bash
dotnet run --project apps/api/tests/GameGuild.Identity.Authorization.PerformanceTests/GameGuild.Identity.Authorization.PerformanceTests.csproj --configuration Release -- --filter '*PermissionCacheDatabaseLookupBenchmarks*' --iterationCount 10 --warmupCount 8 --launchCount 1 --artifacts "${TMPDIR:-/tmp}/gameguild-authorization-cache-benchmarks"
```

```bash
dotnet run --project apps/api/tests/GameGuild.Identity.Authorization.PerformanceTests/GameGuild.Identity.Authorization.PerformanceTests.csproj --configuration Release -- --filter '*PermissionCacheEfDatabaseLookupBenchmarks*' --iterationCount 20 --warmupCount 10 --launchCount 3 --artifacts "${TMPDIR:-/tmp}/gameguild-authorization-ef-cache-benchmarks"
```

Benchmark results describe this isolated local fixture. Use a production-like database, data distribution, deployment topology, and repeated runs before using them for capacity planning.
