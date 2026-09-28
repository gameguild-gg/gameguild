# Issue 149 rate-limit performance evidence

Run on 2026-09-27 with .NET 10.0.10, BenchmarkDotNet 0.14.0, Windows 10, and an Intel Xeon W-2235 host reporting one available CPU.

Command:

```powershell
dotnet run --no-restore --project apps/api/tests/GameGuild.API.RateLimiting.PerformanceTests/GameGuild.API.RateLimiting.PerformanceTests.csproj --configuration Release -- --filter "*RateLimitingRequestBenchmarks*"
```

The `MediumRun` used 15 measured iterations and 2 launches per scenario, after 10 warmups. The benchmark compared identical TestServer request paths with and without the configured in-memory global limiter:

| Scenario | Mean | 99.9% confidence interval | Managed allocation |
| --- | ---: | ---: | ---: |
| Without rate limiting | 10.07 µs/request | 9.081–11.050 µs | 7.51 KB/request |
| With in-memory global rate limiting | 12.10 µs/request | 10.716–13.491 µs | 8.84 KB/request |

The observed mean difference was about 2.04 µs/request (about 20% of the baseline mean), with about 1.33 KB additional managed allocation. The confidence intervals overlap, and BenchmarkDotNet reported short measured iterations and outliers. Treat this run as diagnostic evidence that the benchmark exercises the configured limiter, not as a production SLO or a statistically conclusive overhead claim.

The benchmark excludes authentication, database access, network transport, and Redis. Redis failure behavior is now configurable through `RateLimiting:RedisFailureMode`: `FailOpen` preserves the current availability-first behavior and `FailClosed` returns HTTP 503 Problem Details when admission cannot be checked. A Testcontainers integration test now validates the shared request limit across two independent Kestrel OS processes under 200 concurrent requests. Issue 149 remains open for representative production profiling and an operator-selected deployment mode.
