using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromTypes(
    [
        typeof(GameGuild.Identity.Authorization.PerformanceTests.PermissionCacheLookupBenchmarks),
        typeof(GameGuild.Identity.Authorization.PerformanceTests.PermissionCacheDatabaseLookupBenchmarks)
    ])
    .Run(args);
