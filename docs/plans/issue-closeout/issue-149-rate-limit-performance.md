# Issue 149 rate-limit performance evidence

Run on 2026-09-28 with .NET 10.0.10, BenchmarkDotNet 0.14.0, Windows 10, and an Intel Xeon W-2235 host reporting one available CPU (12 logical, 6 physical cores).

Command:

```powershell
dotnet run --no-restore --project apps/api/tests/GameGuild.API.RateLimiting.PerformanceTests/GameGuild.API.RateLimiting.PerformanceTests.csproj --configuration Release -- --filter "*RateLimitingRequestBenchmarks*"
```

The `MediumRun` used 15 measured iterations and 2 launches per scenario, after 10 warmups. The benchmark compared identical TestServer request paths with and without the configured in-memory global limiter:

| Scenario | Mean | 99.9% confidence interval | Managed allocation |
| --- | ---: | ---: | ---: |
| Without rate limiting | 7.790 µs/request | 7.676–7.904 µs | 7.51 KB/request |
| With in-memory global rate limiting | 9.441 µs/request | 9.229–9.654 µs | 8.83 KB/request |

The observed mean difference was 1.651 µs/request (about 21.2% of the baseline mean), with 1.32 KB additional managed allocation. BenchmarkDotNet reported one outlier in the unthrottled scenario and two in the limited scenario. The intervals do not overlap for this local run, but the TestServer workload and constrained host do not establish production latency or capacity. Treat it as a repeatable diagnostic comparison, not as a production SLO.

The benchmark excludes authentication, database access, network transport, and Redis. Redis failure behavior is configurable through `RateLimiting:RedisFailureMode`: `FailOpen` preserves availability and `FailClosed` returns HTTP 503 Problem Details when admission cannot be checked. Post-sync verification on 2026-09-29 passed 28 focused rate-limit unit tests, 14 Redis failure-mode tests, and 16 Redis Testcontainers integration tests, including shared limits across independent Kestrel hosts under concurrent load. The issue Acceptance Criteria and Definition of Done require in-memory and Redis storage; database-backed storage is mentioned only in the proposed solution. It specifies no production SLO or operator-selected failure mode; representative production profiling is a deployment follow-up.
