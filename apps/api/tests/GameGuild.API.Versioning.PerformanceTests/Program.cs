using BenchmarkDotNet.Running;

namespace GameGuild.API.Versioning.PerformanceTests;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(ApiVersioningResolutionBenchmarks).Assembly).Run(args);
    }
}
