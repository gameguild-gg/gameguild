using BenchmarkDotNet.Running;
using GameGuild.Identity.Authentication.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(PermissionBulkCheckBenchmarks).Assembly).Run(args);
