# Local stash reconciliation (2026-09-30)

The three remaining local stashes were created by Matheus Martins during the
authorization and rate-limiting work. Their source changes and benchmark data
were compared with `develop` before the stashes were retired. The generated
`BenchmarkDotNet.Artifacts/` directory is ignored by Git; the measured results
and their limitations are preserved here instead of committing generated HTML,
CSV, and run logs.

| Stash commit | Original branch | Tracked source changes | Result |
| --- | --- | --- | --- |
| `41e88556cc9d2a5fe9638e1358fdd2d93ba8636d` | `feature/permission-cache-invalidation` | None | L1 lookup microbenchmark recorded below. |
| `dd994a1c7fd8e1e3de49d2f0eb5e4fd4ef4c8eb3` | `feature/permission-cache-invalidation-clean` | Three authorization performance-test files | All three files are byte-for-byte identical to `develop`; the rate-limiter measurements are already in [issue 149 performance evidence](issue-149-rate-limit-performance.md). |
| `4ec849016711eb206ac87e124cc9e04b69aafe98` | `feature/bulk-permission-checks` | None | Earlier exploratory bulk-check results recorded below. A later run and its interpretation are in [bulk permission checks](../../architecture/bulk-permission-checks.md). |

The source comparison for the middle stash used `git diff develop
dd994a1c7fd8e1e3de49d2f0eb5e4fd4ef4c8eb3 --` against its three changed
performance-test paths; it produced no diff. Applying that stash would therefore
reintroduce only ignored generated files. No pending source implementation was
found in any of the three stashes.

## Authorization L1 microbenchmark

The first stash holds a BenchmarkDotNet 0.14.0 `ShortRun` report from Windows 10,
.NET 10.0.10, and an Intel Xeon W-2235. It used one launch, three warmups, and
three measured iterations. Reported means compare a trivial direct in-memory
permission lookup with a warm hybrid-cache L1 lookup:

| Working set | Direct lookup | Warm L1 lookup | L1/direct ratio | L1 allocation |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 41.78 ns | 222.28 ns | 5.32 | 96 B |
| 256 | 45.88 ns | 218.16 ns | 4.75 | 96 B |
| 4,096 | 53.43 ns | 216.40 ns | 4.05 | 96 B |

This measures the overhead of the cache path against an already-resident value.
It does not compare that cache with a database or Redis lookup and cannot prove
a production latency improvement. The database-backed comparisons and their
limitations are documented in the [authorization cache benchmark README](../../../apps/api/tests/GameGuild.Identity.Authorization.PerformanceTests/README.md).

## Earlier bulk-check exploratory run

The third stash holds a BenchmarkDotNet `Dry` run with one cold-start measured
iteration per case, on the same Windows/.NET host. Its results were:

| Users | Individual checks | Bulk matrix | Streamed checks |
| ---: | ---: | ---: | ---: |
| 100 | 255.2 ms | 174.1 ms | 181.6 ms |
| 1,000 | 3,865.6 ms | 181.2 ms | 239.5 ms |

The corresponding allocations at 1,000 users were 1,864,638.95 KB,
1,635.6 KB, and 5,835.59 KB respectively. As a single cold-start run using
the in-memory fixture, these values are exploratory. The later run in the
architecture document is the maintained comparison; neither run sets a
production latency target.

## Rate-limiter benchmark

The second stash's `MediumRun` report measured 7.790 µs per TestServer request
without the in-memory global limiter and 9.441 µs with it. Its host, iteration
count, confidence intervals, allocation data, and limitations were already
recorded in [issue 149 performance evidence](issue-149-rate-limit-performance.md).
