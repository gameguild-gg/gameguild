using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace GameGuild.Commerce.Payments.IntegrationTests;

/// <summary>
/// Integration tests for Wallet API endpoints
/// </summary>
public class WalletEndpointsIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _client;
    private static readonly string DatabaseName = $"PaymentsWalletTestDb_{Guid.NewGuid()}";

    public WalletEndpointsIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Remove all EF Core and Npgsql service registrations
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                                d.ServiceType == typeof(ApplicationDbContext) ||
                                d.ServiceType.FullName?.Contains("EntityFramework") == true ||
                                d.ImplementationType?.FullName?.Contains("Npgsql") == true)
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(DatabaseName);
                });

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateWallet_ShouldReturn200_WithValidRequest()
    {
        // Arrange
        var request = new
        {
            UserId = Guid.NewGuid(),
            Currency = "USD"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/wallets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateWallet_ShouldReturnBadRequest_WithInvalidUserId()
    {
        // Arrange
        var request = new
        {
            UserId = Guid.Empty,
            Currency = "USD"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/wallets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWallet_ShouldReturn404_WhenWalletNotFound()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{nonExistentUserId}/wallet");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAndGetWallet_ShouldReturnWallet()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createRequest = new
        {
            UserId = userId,
            Currency = "USD"
        };

        // Act - Create wallet
        var createResponse = await _client.PostAsJsonAsync("/api/v1/wallets", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Act - Get wallet
        var getResponse = await _client.GetAsync($"/api/v1/users/{userId}/wallet");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await getResponse.Content.ReadAsStringAsync();
        content.Should().Contain(userId.ToString());
    }

    [Fact]
    public async Task GetBalance_ShouldReturnZero_ForNewWallet()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createRequest = new
        {
            UserId = userId,
            Currency = "USD"
        };

        await _client.PostAsJsonAsync("/api/v1/wallets", createRequest);

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{userId}/wallet/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balance = await response.Content.ReadFromJsonAsync<decimal>();
        balance.Should().Be(0);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
