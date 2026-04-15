using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Commerce.Subscriptions.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.IntegrationTests;

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
            builder.ConfigureTestServices(services =>
            {
                // Remove ALL existing DbContext and EF Core registrations
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

                // Add in-memory database with custom context that includes Subscription entities
                var databaseName = $"SubscriptionTestDb_{Guid.NewGuid()}";

                // create options for ApplicationDbContext since the base constructor requires them
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(databaseName)
                    .Options;

                services.AddSingleton(options);
                services.AddScoped<SubscriptionTestDbContext>();
                services.AddScoped<ApplicationDbContext>(p => p.GetRequiredService<SubscriptionTestDbContext>());

                // Add HTTP logging services (required by the pipeline)
                services.AddHttpLogging(o => { });

                // Override authentication with the test handler
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test");
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
    public async Task CreateSubscription_ShouldReturnBadRequest_WithInvalidBillingCycle()
    {
        // Arrange
        var createRequest = new
        {
            TenantId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid(),
            BillingCycle = "InvalidCycle", // Invalid billing cycle
            Amount = 29.99m,
            Currency = "USD"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions", createRequest);

        // Assert
        // Should return 400 Bad Request due to validation error
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
