using BenchmarkDotNet.Attributes;
using GameGuild.API.Setup;
using GameGuild.Configuration.PresentationLayer.OpenAPI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;

namespace GameGuild.API.Versioning.PerformanceTests;

/// <summary>Compares full document generation with default and configured metadata.</summary>
[MemoryDiagnoser]
public class OpenApiDocumentGenerationBenchmarks
{
    private WebApplication _defaultApp = null!;
    private WebApplication _configuredApp = null!;
    private ISwaggerProvider _defaultProvider = null!;
    private ISwaggerProvider _configuredProvider = null!;

    [GlobalSetup]
    public void Setup()
    {
        _defaultApp = CreateApp(new OpenApiOptions());
        _configuredApp = CreateApp(new OpenApiOptions
        {
            Extensions = new Dictionary<string, string>
            {
                ["x-api-audience"] = "{\"roles\":[\"developer\",\"operator\"],\"public\":true}"
            },
            Schemas = new Dictionary<string, OpenApiSchemaDocumentationOptions>
            {
                ["Identity_Users_UserDto"] = new()
                {
                    Description = "Documented account profile.",
                    ExampleJson = "{\"id\":\"00000000-0000-0000-0000-000000000001\",\"email\":\"ada@example.com\",\"name\":\"Ada\",\"createdAt\":\"2026-09-30T12:00:00Z\"}"
                }
            }
        });
        _defaultProvider = _defaultApp.Services.GetRequiredService<ISwaggerProvider>();
        _configuredProvider = _configuredApp.Services.GetRequiredService<ISwaggerProvider>();
        if (_defaultProvider.GetSwagger("v1").Paths.Count == 0 || _configuredProvider.GetSwagger("v1").Paths.Count == 0)
        {
            throw new InvalidOperationException("OpenAPI benchmark requires a non-empty API document.");
        }
    }

    [Benchmark(Baseline = true)]
    public OpenApiDocument DefaultDocument() => _defaultProvider.GetSwagger("v1");

    [Benchmark]
    public OpenApiDocument ConfiguredDocument() => _configuredProvider.GetSwagger("v1");

    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        await _defaultApp.DisposeAsync().ConfigureAwait(false);
        await _configuredApp.DisposeAsync().ConfigureAwait(false);
    }

    private static WebApplication CreateApp(OpenApiOptions options)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(OpenApiExtensions).Assembly.GetName().Name,
            EnvironmentName = "Testing",
            ContentRootPath = AppContext.BaseDirectory,
            Args = []
        });
        builder.Configuration.Sources.Clear();
        builder.Services.SetupControllers(builder.Configuration, null);
        builder.Services.SetupApiVersioning(builder.Configuration, null);
        builder.Services.SetupApiExplorer(builder.Configuration, null);
        builder.Services.SetupOpenApi(builder.Configuration, options);
        return builder.Build();
    }
}
