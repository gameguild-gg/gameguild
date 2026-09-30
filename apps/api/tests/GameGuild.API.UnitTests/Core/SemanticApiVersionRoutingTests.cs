using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Asp.Versioning;
using GameGuild.API.Core.ApiVersioning;
using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.ApiVersioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedApiVersioningOptions = GameGuild.Configuration.PresentationLayer.ApiVersioning.ApiVersioningOptions;

namespace GameGuild.API.UnitTests.Core;

[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("1.1")]
[Route("api/v{version:apiVersion}/semantic-routing")]
public sealed class NativeVersionRoutingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("native");
}

[ApiController]
[SemanticApiVersion("1.2.3")]
[Route("api/v{version:apiVersion}/semantic-routing")]
public sealed class SemanticVersionRoutingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("semantic");
}

public sealed class SemanticApiVersionRoutingTests
{
    [Fact]
    public async Task UrlSegment_RoutesNativeAndSemanticVersionsAndRejectsUnknownPatch()
    {
        var options = SharedApiVersioningOptions.CreateDefault();
        options.VersionFormat = ApiVersionFormatKind.SemanticVersion;
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
        options.CompatibilityMatrix["1.2.3"] = ["1.2.2"];
        options.SunsetPolicies["1.0"] = new()
        {
            EffectiveAt = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero),
            PolicyUrl = "https://docs.example.com/api/sunset"
        };
        var recordedRequests = new ConcurrentBag<Dictionary<string, string>>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == ApiVersionUsageMetrics.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            if (instrument.Name == "gameguild.api.versioning.requests")
            {
                recordedRequests.Add(tags.ToArray().ToDictionary(
                    tag => tag.Key,
                    tag => tag.Value?.ToString() ?? string.Empty));
            }
        });
        meterListener.Start();

        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.ConfigureServices(services =>
            {
                services.AddRouting();
                services.SetupApiVersioning(new ConfigurationBuilder().Build(), options);
                services.AddControllers().AddApplicationPart(typeof(SemanticVersionRoutingController).Assembly);
            });
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseMiddleware<ApiVersionUsageMiddleware>();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            });
        });

        using var host = await builder.StartAsync();
        using var client = host.GetTestClient();

        var native = await client.GetAsync("/api/v1.0/semantic-routing");
        var semanticPatchZero = await client.GetAsync("/api/v1.0.0/semantic-routing");
        var semantic = await client.GetAsync("/api/v1.2.3/semantic-routing");
        var unknownPatch = await client.GetAsync("/api/v1.2.4/semantic-routing");

        native.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        (await native.Content.ReadAsStringAsync()).Should().Be("native");
        native.Headers.Contains("api-supported-versions").Should().BeTrue(
            "the response headers were {0}",
            string.Join(", ", native.Headers.Concat(native.Content.Headers).Select(header => header.Key)));
        native.Headers.Contains("api-deprecated-versions").Should().BeTrue();
        native.Headers.Contains("Sunset").Should().BeTrue();
        native.Headers.Contains("Link").Should().BeTrue();
        semanticPatchZero.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        (await semanticPatchZero.Content.ReadAsStringAsync()).Should().Be("native");
        semantic.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        (await semantic.Content.ReadAsStringAsync()).Should().Be("semantic");
        semantic.Headers.GetValues(ApiVersionUsageMiddleware.CompatibleVersionsHeaderName)
            .Should().ContainSingle().Which.Should().Be("1.2.2");
        unknownPatch.StatusCode.Should().NotBe(System.Net.HttpStatusCode.OK);
        recordedRequests.Should().Contain(request =>
            request["api.version"] == "1.2.3" &&
            request["http.route"] == "api/v{version:apiVersion}/semantic-routing" &&
            request["http.request.method"] == "GET" &&
            request["http.response.status_code"] == "200");
        recordedRequests.Should().NotContain(request => request["api.version"] == "1.2.4",
            "unmatched client input must not create unbounded version metric dimensions");
    }
}
