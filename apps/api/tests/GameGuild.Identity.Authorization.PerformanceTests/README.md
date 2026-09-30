# Authorization cache benchmarks

This project contains three complementary benchmarks:

- `PermissionCacheLookupBenchmarks` compares a direct in-memory permission lookup with a warm L1 cache lookup.
- `PermissionCacheDatabaseLookupBenchmarks` compares database-backed and warm cached ACL batches at concurrency levels 1, 8, and 32.
- `PermissionCacheEfDatabaseLookupBenchmarks` compares the current ACL repository through EF Core/Npgsql against a warm L1 cache while retaining the database-backed tenant/global security-version lookup.

The raw-SQL database benchmark starts a disposable PostgreSQL 16 container, creates indexed ACL and tenant-version tables, and seeds 5,000 unrelated ACL entries plus the grant under test. It exercises `DatabaseAccessControlListService` and `CachedAccessControlListService`; the ACL repository adapter runs a PostgreSQL query with the same tenant, resource, active, expiry, and principal filters as `AccessControlListEntryRepository`. The cached case includes the production `DatabaseTenantSecurityVersionStore` lookup of tenant and global versions, then measures a warm per-process L1 ACL lookup. Each database access uses a pooled connection, so independent lookups can execute concurrently. This fixture omits EF materialization, Redis, production data distribution, and networked application deployment. Treat it as a local comparative fixture, not a production capacity result.

## Local comparison (2026-09-29)

ShortRun used 10 warmup iterations and 10 measured iterations on Windows 10, .NET 10.0.10, with Docker PostgreSQL 16. The values are total time for each concurrent batch, not per-request latency:

| Concurrent lookups per batch | Database ACL batch | Warm cached ACL batch | Cache/database ratio |
| ---: | ---: | ---: | ---: |
| 1 | 763.6 µs | 673.6 µs | 0.89 |
| 8 | 2.312 ms | 3.218 ms | 1.43 |
| 32 | 9.068 ms | 7.031 ms | 0.82 |

The 99.9% confidence intervals overlap widely at every concurrency level. This raw-SQL run does not establish a consistent performance improvement; see the EF-backed comparison below, which also does not prove a production latency gain.

## EF-backed local comparison (2026-09-29)

The EF benchmark uses the current `AccessControlListEntryRepository` and `TenantSecurityVersionRepository` with a request-scoped test `DbContext` per lookup. It starts PostgreSQL 16 in Docker, seeds 5,000 unrelated ACL rows plus the grant under test, and compares database ACL reads with a warm process-local L1 ACL cache. Both paths read the tenant/global security versions from PostgreSQL. This test fixture does not run the full API `ApplicationDbContext`, Redis, production data distribution, or a networked application deployment.

BenchmarkDotNet 0.14.0 ran 10 warmup and 10 measured iterations on Windows 10, .NET 10.0.10, with one launch. Values are total time and managed allocation for a concurrent batch. The reported latency intervals are 99.9% confidence intervals; BenchmarkDotNet removed one outlier in four of the six cases.

| Concurrent lookups per batch | PostgreSQL + EF batch | Warm L1 + EF version-read batch | Cache/database latency ratio | Managed allocation ratio |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 1.744 ms ± 0.309 ms | 1.811 ms ± 0.451 ms | 1.04 | 0.77 |
| 8 | 5.209 ms ± 1.773 ms | 8.965 ms ± 2.125 ms | 1.72 | 0.77 |
| 32 | 34.360 ms ± 6.727 ms | 26.564 ms ± 8.913 ms | 0.77 | 0.77 |

The confidence intervals overlap at all three concurrency levels, and the warm-cache path is slower at 1 and 8 lookups in this run. The cache reduced measured managed allocation by about 23% per batch, but these results do not establish a consistent latency improvement or production capacity. Repeat against the deployed topology and representative data before accepting the performance criterion.

Run the database benchmark with Docker available:

```powershell
dotnet run --project apps/api/tests/GameGuild.Identity.Authorization.PerformanceTests/GameGuild.Identity.Authorization.PerformanceTests.csproj --configuration Release -- --filter "*PermissionCacheDatabaseLookupBenchmarks*" --iterationCount 10 --warmupCount 10 --launchCount 1 --artifacts "$env:TEMP\GameGuild.AuthorizationCacheBenchmarks"
```

Run the EF-backed benchmark with Docker available:

```powershell
dotnet run --project apps/api/tests/GameGuild.Identity.Authorization.PerformanceTests/GameGuild.Identity.Authorization.PerformanceTests.csproj --configuration Release -- --filter "*PermissionCacheEfDatabaseLookupBenchmarks*" --iterationCount 10 --warmupCount 10 --launchCount 1 --artifacts "$env:TEMP\GameGuild.AuthorizationEfCacheBenchmarks"
```

```bash
dotnet run --project apps/api/tests/GameGuild.Identity.Authorization.PerformanceTests/GameGuild.Identity.Authorization.PerformanceTests.csproj --configuration Release -- --filter '*PermissionCacheDatabaseLookupBenchmarks*' --iterationCount 10 --warmupCount 10 --launchCount 1 --artifacts "${TMPDIR:-/tmp}/gameguild-authorization-cache-benchmarks"
```

```bash
dotnet run --project apps/api/tests/GameGuild.Identity.Authorization.PerformanceTests/GameGuild.Identity.Authorization.PerformanceTests.csproj --configuration Release -- --filter '*PermissionCacheEfDatabaseLookupBenchmarks*' --iterationCount 10 --warmupCount 10 --launchCount 1 --artifacts "${TMPDIR:-/tmp}/gameguild-authorization-ef-cache-benchmarks"
```

Benchmark results describe this isolated local fixture. Use a production-like database, data distribution, deployment topology, and repeated runs before using them for capacity planning.
