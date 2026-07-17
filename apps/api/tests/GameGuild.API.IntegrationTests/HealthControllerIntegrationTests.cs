using FluentAssertions;
using GameGuild.API.Controllers;
using GameGuild.API.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace GameGuild.API.IntegrationTests;

/// <summary>
/// Integration tests for API HealthController endpoints
/// </summary>
[Collection(ApiPostgreSqlCollection.Name)]
public class HealthControllerIntegrationTests : IDisposable
{
    private readonly HttpClient _client;

    public HealthControllerIntegrationTests(ApiPostgreSqlFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturn200WithHealthyDatabaseStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<HealthinessResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Healthy");
        result.Checks.Should().ContainKey("database").WhoseValue.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealth_ShouldReturnJsonContentType()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetHealth_ShouldReturnHealthCheckResponse()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Should().NotBeNull();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("status");
        content.Should().Contain("timestamp");
    }

    [Fact]
    public async Task GetReadiness_ShouldReturn200WithReadyStatus()
    {
        // Act
        var response = await _client.GetAsync("/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ReadinessResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Healthy");
        result.Ready.Should().BeTrue();
        result.Services.Should().ContainKey("database").WhoseValue.Should().BeTrue();
    }

    [Fact]
    public async Task GetLiveness_ShouldReturn200WithAliveStatus()
    {
        // Act
        var response = await _client.GetAsync("/live");

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
        var response1 = await _client.GetAsync("/ready");
        var response2 = await _client.GetAsync("/ready");
        var response3 = await _client.GetAsync("/ready");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLiveness_MultipleCalls_ShouldReturnConsistentResults()
    {
        // Act
        var response1 = await _client.GetAsync("/live");
        var response2 = await _client.GetAsync("/live");
        var response3 = await _client.GetAsync("/live");

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
