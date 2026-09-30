using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using GameGuild.API;
using ProblemDetailsOptions = GameGuild.Configuration.PresentationLayer.ProblemDetails.ProblemDetailsOptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GameGuild.API.Versioning.PerformanceTests;

/// <summary>Measures the additional cost of configured Problem Details formatting and serialization.</summary>
[MemoryDiagnoser]
public class ProblemDetailsGenerationBenchmarks
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private HttpContext _httpContext = null!;
    private Action<ProblemDetailsContext> _customize = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.SetupProblemDetails(new ConfigurationBuilder().Build(), ProblemDetailsOptions.CreateDefault());
        var provider = services.BuildServiceProvider();
        _httpContext = new DefaultHttpContext
        {
            RequestServices = provider,
            TraceIdentifier = "problem-details-benchmark",
        };
        _httpContext.Request.Path = "/api/benchmark";
        _customize = provider
            .GetRequiredService<IOptions<Microsoft.AspNetCore.Http.ProblemDetailsOptions>>()
            .Value.CustomizeProblemDetails!;
    }

    [Benchmark(Baseline = true, Description = "Map and serialize domain error")]
    public string MapAndSerializeDomainError()
    {
        var problem = ProblemDetailsMapper.ToProblemDetails(Error.NotFound("Resource.NotFound", "Resource not found."));
        return JsonSerializer.Serialize(problem, JsonOptions);
    }

    [Benchmark(Description = "Map, configure, and serialize Problem Details")]
    public string ConfigureAndSerializeProblemDetails()
    {
        var problem = ProblemDetailsMapper.ToProblemDetails(Error.NotFound("Resource.NotFound", "Resource not found."));
        _customize(new ProblemDetailsContext { HttpContext = _httpContext, ProblemDetails = problem });
        return JsonSerializer.Serialize(problem, JsonOptions);
    }
}
