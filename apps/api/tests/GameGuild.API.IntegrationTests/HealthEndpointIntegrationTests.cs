using FluentAssertions;
using GameGuild.API.Controllers;
using GameGuild.API.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace GameGuild.API.IntegrationTests;

/// <summary>
/// Integration tests for API health endpoints
/// </summary>
[Collection(ApiPostgreSqlCollection.Name)]
public class HealthEndpointIntegrationTests : IDisposable
{
    private readonly HttpClient _client;

    public HealthEndpointIntegrationTests(ApiPostgreSqlFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturn200_WithHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<HealthinessResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Healthy");
        result.Checks.Should().ContainKey("database").WhoseValue.Status.Should().Be("Healthy");
        result.ReleaseSha.Should().NotBeNullOrWhiteSpace();
        response.Headers.GetValues("X-GameGuild-Release-Sha").Should().ContainSingle(result.ReleaseSha);
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
    public async Task GetHealth_ShouldIncludeStatusInformation()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.Should().NotBeNull();
        content.Should().Contain("status");
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task GetHealth_ShouldIncludeTimestamp()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("timestamp");
    }

    [Fact]
    public async Task GetHealth_MultipleCalls_ShouldReturnConsistentResults()
    {
        // Act
        var response1 = await _client.GetAsync("/health");
        var response2 = await _client.GetAsync("/health");
        var response3 = await _client.GetAsync("/health");

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
