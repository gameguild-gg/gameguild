using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(GameGuild.Identity.Authorization.PerformanceTests.PermissionCacheLookupBenchmarks).Assembly).Run(args);
