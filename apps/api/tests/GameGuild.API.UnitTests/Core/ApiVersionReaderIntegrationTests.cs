using Asp.Versioning;
using FluentAssertions;
using GameGuild.API.Core.ApiVersioning;
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
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/version-reader")]
[Route("api/v{version:apiVersion}/version-reader")]
public sealed class ApiVersionReaderController : ControllerBase
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    public IActionResult GetV1() => Content("1.0");

    [HttpGet]
    [MapToApiVersion("2.0")]
    public IActionResult GetV2() => Content("2.0");
}

public sealed class ApiVersionReaderIntegrationTests
{
    [Theory]
    [InlineData(ApiVersionReadingStrategy.QueryString, "/api/version-reader?version=2.0", null, null)]
    [InlineData(ApiVersionReadingStrategy.Header, "/api/version-reader", "X-Version", "2.0")]
    [InlineData(ApiVersionReadingStrategy.MediaType, "/api/version-reader", "Accept", "application/json;ver=2.0")]
    [InlineData(ApiVersionReadingStrategy.UrlSegmentAndHeader, "/api/v2.0/version-reader", "X-Version", "2.0")]
    public async Task ConfiguredReader_RoutesToRequestedVersion(
        ApiVersionReadingStrategy strategy,
        string requestPath,
        string? headerName,
        string? headerValue)
    {
        using var host = await CreateHostAsync(strategy);
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, requestPath);

        if (headerName is not null && headerValue is not null)
        {
            request.Headers.TryAddWithoutValidation(headerName, headerValue).Should().BeTrue();
        }

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("2.0");
    }

    [Fact]
    public async Task CombinedReaders_RejectConflictingVersionValues()
    {
        using var host = await CreateHostAsync(ApiVersionReadingStrategy.UrlSegmentAndHeader);
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2.0/version-reader");
        request.Headers.Add("X-Version", "1.0");

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.OK);
    }

    private static async Task<IHost> CreateHostAsync(ApiVersionReadingStrategy strategy)
    {
        var options = SharedApiVersioningOptions.CreateDefault();
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReadingStrategy = strategy;

        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.ConfigureServices(services =>
            {
                services.AddRouting();
                services.SetupApiVersioning(new ConfigurationBuilder().Build(), options);
                services.AddControllers().AddApplicationPart(typeof(ApiVersionReaderController).Assembly);
            });
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            });
        });

        var host = await builder.StartAsync();
        return host;
    }
}