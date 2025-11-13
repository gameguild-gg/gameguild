using FluentAssertions;
using GameGuild.API.Controllers;
using GameGuild.API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GameGuild.API.IntegrationTests;

/// <summary>
/// Integration tests for API HealthController endpoints
/// </summary>
public class HealthControllerIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _client;

    public HealthControllerIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registrations
                var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                var dbContextDescriptor2 = services.SingleOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
                if (dbContextDescriptor2 != null)
                {
                    services.Remove(dbContextDescriptor2);
                }

                // Add in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"HealthControllerTestDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturn200OrHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        // Health endpoint returns 200 when healthy or 503 when unhealthy
        var statusCode = response.StatusCode;
        (statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.ServiceUnavailable).Should().BeTrue(
            $"Health endpoint should return either 200 or 503, but returned {(int)statusCode}");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnJsonContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetHealth_ShouldReturnHealthCheckResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.Should().NotBeNull();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Status");
        content.Should().Contain("Timestamp");
    }

    [Fact]
    public async Task GetReadiness_ShouldReturn200WithReadyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ReadinessResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Healthy");
        result.Ready.Should().BeTrue();
    }

    [Fact]
    public async Task GetLiveness_ShouldReturn200WithAliveStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<LivenessResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Healthy");
        result.Alive.Should().BeTrue();
    }

    [Fact]
    public async Task GetReadiness_MultipleCalls_ShouldReturnConsistentResults()
    {
        // Act
        var response1 = await _client.GetAsync("/api/health/ready");
        var response2 = await _client.GetAsync("/api/health/ready");
        var response3 = await _client.GetAsync("/api/health/ready");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLiveness_MultipleCalls_ShouldReturnConsistentResults()
    {
        // Act
        var response1 = await _client.GetAsync("/api/health/live");
        var response2 = await _client.GetAsync("/api/health/live");
        var response3 = await _client.GetAsync("/api/health/live");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
