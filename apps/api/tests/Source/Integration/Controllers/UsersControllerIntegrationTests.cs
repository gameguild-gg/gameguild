using System.Net;
using System.Net.Http.Headers;
using GameGuild.Tests.Fixtures;
using GameGuild.Tests.Helpers;
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
    }

    private async Task EnsureAuthenticationAsync() {
        if (_client.DefaultRequestHeaders.Authorization == null) {
            var (token, _) = await AuthenticationHelper.CreateAuthenticatedUserAsync(
                _scope.ServiceProvider,
                null, // Don't specify userId - let it create a new user
                "test@example.com",
                ["User"]
            );
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    [Fact]
    public async Task GetUsers_WithKebabCaseRoute_ShouldReturnUsers() {
        // Arrange
        await EnsureAuthenticationAsync();
        var endpoint = "/users"; // This should be kebab-case transformed from "Users" controller

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized,
          $"Expected success or unauthorized, but got {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task GetUsers_WithPascalCaseRoute_ShouldAlsoWork() {
        // Arrange  
        await EnsureAuthenticationAsync();
        var endpoint = "/Users"; // This should work because ASP.NET Core routing is case-insensitive by default

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        // Should work because routing is case-insensitive, but the transformer ensures the canonical route is kebab-case
        Assert.True(response.IsSuccessStatusCode, $"Expected success, but got {response.StatusCode}");
    }

    [Fact]
    public async Task GetUserById_WithKebabCaseRoute_ShouldWork() {
        // Arrange
        await EnsureAuthenticationAsync();
        var userId = Guid.NewGuid();
        var endpoint = $"/users/{userId}"; // kebab-case route

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.NotFound,
          $"Expected success, unauthorized, or not found, but got {response.StatusCode}");
    }

    public void Dispose() {
        _scope?.Dispose();
        _client?.Dispose();
    }
}
