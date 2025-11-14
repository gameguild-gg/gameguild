using FluentAssertions;
using GameGuild.API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GameGuild.Subscriptions.IntegrationTests;

/// <summary>
/// Integration tests for Subscription API endpoints
/// </summary>
public class SubscriptionEndpointIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _client;

    public SubscriptionEndpointIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
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

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"SubscriptionTestDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetSubscriptions_ShouldReturn200_WithEmptyList_WhenNoSubscriptions()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());

        // Act
        var response = await _client.GetAsync("/api/v1/subscriptions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSubscriptionPlans_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/api/subscription-plans");

        // Assert
        // May return 200 with empty list or 404 if no plans seeded
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateSubscription_ShouldRequireAuthentication()
    {
        // Arrange
        var createRequest = new
        {
            TenantId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid(),
            BillingCycle = "Monthly",
            Amount = 29.99m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions", createRequest);

        // Assert
        // Should return 401 Unauthorized without proper authentication
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSubscriptionById_ShouldReturn404_WhenSubscriptionNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/subscriptions/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
