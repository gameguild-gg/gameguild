using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

var config = new ManualConfig();
config.Add(DefaultConfig.Instance);
config.BuildTimeout = TimeSpan.FromMinutes(10);

BenchmarkSwitcher
    .FromAssembly(typeof(GameGuild.API.PerformanceTests.RateLimitingRequestBenchmarks).Assembly)
    .Run(args, config);
