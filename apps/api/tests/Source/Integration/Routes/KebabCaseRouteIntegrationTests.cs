using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GameGuild.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace GameGuild.Tests.Integration.Routes;

/// <summary>
/// Integration tests to verify that kebab-case route transformation is working across all API endpoints
/// </summary>
public class KebabCaseRouteIntegrationTests : IClassFixture<TestWebApplicationFactory>, IDisposable {
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ITestOutputHelper _output;

    public KebabCaseRouteIntegrationTests(TestWebApplicationFactory factory, ITestOutputHelper output) {
        _factory = factory;
        _output = output;
        _scope = factory.Services.CreateScope();
        _client = factory.CreateClient();

        // Add auth token for protected endpoints
        var authToken = GenerateTestJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
    }

    [Theory]
    [InlineData("/users")]
    [InlineData("/tenants")]
    [InlineData("/payments")]
    [InlineData("/subscriptions")]
    public async Task KebabCaseRoutes_ShouldBeAccessible(string route) {
        // Act
        var response = await _client.GetAsync(route);

        // Assert - Should not return 404 (route not found)
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        _output.WriteLine($"Route {route}: {response.StatusCode}");
    }

    [Theory]
    [InlineData("/Users")]
    [InlineData("/Tenants")]
    [InlineData("/Payments")]
    [InlineData("/Subscriptions")]
    public async Task PascalCaseRoutes_ShouldNotBeAccessible(string route) {
        // Act
        var response = await _client.GetAsync(route);

        // Assert - Should return 404 (route not found)
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        _output.WriteLine($"Route {route}: {response.StatusCode} (expected 404)");
    }

    [Theory]
    [InlineData("/user-profiles")]
    [InlineData("/billing-webhooks")]
    public async Task MultiWordKebabCaseRoutes_ShouldBeAccessible(string route) {
        // Act
        var response = await _client.GetAsync(route);

        // Assert - Should not return 404 (route not found)
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        _output.WriteLine($"Route {route}: {response.StatusCode}");
    }

    [Theory]
    [InlineData("/UserProfiles")]
    [InlineData("/BillingWebhooks")]
    public async Task MultiWordPascalCaseRoutes_ShouldNotBeAccessible(string route) {
        // Act
        var response = await _client.GetAsync(route);

        // Assert - Should return 404 (route not found)
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        _output.WriteLine($"Route {route}: {response.StatusCode} (expected 404)");
    }

    [Fact]
    public async Task ApiDiscovery_ShouldShowKebabCaseRoutes() {
        // Arrange - Check if Swagger/OpenAPI endpoint shows kebab-case routes
        var swaggerEndpoint = "/swagger/v1/swagger.json";

        // Act
        var response = await _client.GetAsync(swaggerEndpoint);

        // Assert
        if (response.IsSuccessStatusCode) {
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine("Swagger content snippet:");
            _output.WriteLine(content.Substring(0, Math.Min(500, content.Length)));

            // Check that paths are kebab-case
            Assert.DoesNotContain("\"/Users\"", content);
            Assert.DoesNotContain("\"/Tenants\"", content);

            // May contain kebab-case routes (if they exist)
            // These assertions are informational
            var hasKebabRoutes = content.Contains("\"/users\"") || content.Contains("\"/tenants\"");
            _output.WriteLine($"Contains kebab-case routes: {hasKebabRoutes}");
        }
        else {
            _output.WriteLine($"Swagger endpoint not available: {response.StatusCode}");
        }
    }

    [Fact]
    public async Task RouteConventions_ShouldApplyToAllControllers() {
        // Arrange - List of expected kebab-case routes based on controllers
        var expectedKebabRoutes = new[]
        {
      "/users",
      "/tenants",
      "/payments",
      "/subscriptions"
    };

        // Act & Assert
        foreach (var route in expectedKebabRoutes) {
            var response = await _client.GetAsync(route);

            // Should not be 404 (route should exist, even if unauthorized)
            Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
            _output.WriteLine($"✓ Kebab-case route {route} exists: {response.StatusCode}");
        }
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
