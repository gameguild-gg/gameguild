using FluentAssertions;
using GameGuild.API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GameGuild.Payments.IntegrationTests;

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

                // Add in-memory database with shared name for all requests
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(DatabaseName);
                });
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
        var response = await _client.PostAsJsonAsync("/api/v1/wallet/create", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
        var response = await _client.PostAsJsonAsync("/api/v1/wallet/create", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWallet_ShouldReturn404_WhenWalletNotFound()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/wallet/{nonExistentUserId}");

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
        var createResponse = await _client.PostAsJsonAsync("/api/v1/wallet/create", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Act - Get wallet
        var getResponse = await _client.GetAsync($"/api/v1/wallet/{userId}");

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

        await _client.PostAsJsonAsync("/api/v1/wallet/create", createRequest);

        // Act
        var response = await _client.GetAsync($"/api/v1/wallet/{userId}/balance");

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
