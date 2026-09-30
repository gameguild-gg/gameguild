using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Asp.Versioning;
using GameGuild.Configuration.PresentationLayer.ApiVersioning;
using Microsoft.AspNetCore.Http;
using SharedApiVersioningOptions = GameGuild.Configuration.PresentationLayer.ApiVersioning.ApiVersioningOptions;

namespace GameGuild.API.Versioning.PerformanceTests;

[MemoryDiagnoser]
[ShortRunJob]
public class ApiVersioningResolutionBenchmarks
{
    private const string VersionText = "2.0";
    private const string SemanticVersionText = "2.0.0-rc.1";
    private IApiVersionParser _parser = null!;
    private IApiVersionParser _semanticParser = null!;
    private IApiVersionReader _queryReader = null!;
    private IApiVersionReader _headerReader = null!;
    private DefaultHttpContext _queryContext = null!;
    private DefaultHttpContext _headerContext = null!;

    [GlobalSetup]
    public void Setup()
    {
        _parser = ApiVersioningOptionsBuilder.CreateParser(ApiVersionFormatKind.Native);
        _semanticParser = ApiVersioningOptionsBuilder.CreateParser(ApiVersionFormatKind.SemanticVersion);
        var options = SharedApiVersioningOptions.CreateDefault();
        _queryReader = ApiVersioningOptionsBuilder.CreateReader(ApiVersionReadingStrategy.QueryString, options);
        _headerReader = ApiVersioningOptionsBuilder.CreateReader(ApiVersionReadingStrategy.Header, options);
        _queryContext = new DefaultHttpContext();
        _queryContext.Request.QueryString = new QueryString($"?{options.QueryParameterName}={VersionText}");
        _headerContext = new DefaultHttpContext();
        _headerContext.Request.Headers[options.HeaderName] = VersionText;
    }

    [Benchmark(Baseline = true)]
    public ApiVersion ParseVersion() => _parser.Parse(VersionText.AsSpan());

    [Benchmark]
    public ApiVersion ParseSemanticVersion() => _semanticParser.Parse(SemanticVersionText.AsSpan());

    [Benchmark]
    public ApiVersion? ReadAndParseQueryVersion()
    {
        var values = _queryReader.Read(_queryContext.Request);
        return values.Count == 0 ? null : _parser.Parse(values[0].AsSpan());
    }

    [Benchmark]
    public ApiVersion? ReadAndParseHeaderVersion()
    {
        var values = _headerReader.Read(_headerContext.Request);
        return values.Count == 0 ? null : _parser.Parse(values[0].AsSpan());
    }
}
