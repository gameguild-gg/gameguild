using Asp.Versioning;
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
        options.SunsetPolicies["1.0"] = new()
        {
            EffectiveAt = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero),
            PolicyUrl = "https://docs.example.com/api/sunset"
        };

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
        unknownPatch.StatusCode.Should().NotBe(System.Net.HttpStatusCode.OK);
    }
}
