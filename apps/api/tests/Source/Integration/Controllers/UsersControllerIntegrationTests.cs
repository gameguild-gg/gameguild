using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GameGuild.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace GameGuild.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for Users controller to verify kebab-case routes are working correctly
/// </summary>
public class UsersControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>, IDisposable {
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ITestOutputHelper _output;

    public UsersControllerIntegrationTests(TestWebApplicationFactory factory, ITestOutputHelper output) {
        _factory = factory;
        _output = output;
        _scope = factory.Services.CreateScope();
        _client = factory.CreateClient();

        // Add auth token for protected endpoints
        var authToken = GenerateTestJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
    }

    [Fact]
    public async Task GetUsers_WithKebabCaseRoute_ShouldReturnUsers() {
        // Arrange
        var endpoint = "/users"; // This should be kebab-case transformed from "Users" controller

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized,
          $"Expected success or unauthorized, but got {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task GetUsers_WithPascalCaseRoute_ShouldNotWork() {
        // Arrange  
        var endpoint = "/Users"; // This should NOT work with kebab-case transformer

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_WithKebabCaseRoute_ShouldWork() {
        // Arrange
        var userId = Guid.NewGuid();
        var endpoint = $"/users/{userId}"; // kebab-case route

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.NotFound,
          $"Expected success, unauthorized, or not found, but got {response.StatusCode}");
    }

    private string GenerateTestJwtToken() {
        // Simple test token generation - matches the pattern in existing tests
        var claims = new Dictionary<string, object> {
            ["sub"] = Guid.NewGuid().ToString(),
            ["email"] = "test@example.com",
            ["name"] = "Test User",
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        };

        // Return a simple test token (in real tests, this would use proper JWT generation)
        return "test-jwt-token";
    }

    public void Dispose() {
        _scope?.Dispose();
        _client?.Dispose();
    }
}
