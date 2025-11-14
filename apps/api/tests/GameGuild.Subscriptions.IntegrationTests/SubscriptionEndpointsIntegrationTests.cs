using FluentAssertions;
using GameGuild.API.Data;
using GameGuild.Subscriptions.Commands;
using GameGuild.Subscriptions.Models;
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
public class SubscriptionEndpointsIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _client;

    public SubscriptionEndpointsIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
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
                    options.UseInMemoryDatabase($"SubscriptionTestDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    private async Task<Guid> SeedSubscriptionPlanAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var plan = new GameGuild.Subscriptions.SubscriptionPlans.Entities.SubscriptionPlan(
            name: "Test Plan",
            slug: "test-plan",
            monthlyPriceInCents: 2999,
            currency: "USD",
            description: "Test subscription plan"
        );
        
        dbContext.Set<GameGuild.Subscriptions.SubscriptionPlans.Entities.SubscriptionPlan>().Add(plan);
        await dbContext.SaveChangesAsync();
        
        return plan.Id;
    }

    [Fact]
    public async Task CreateSubscription_ShouldReturn201_WithValidCommand()
    {
        // Arrange
        var planId = await SeedSubscriptionPlanAsync();
        var request = new
        {
            TenantId = Guid.NewGuid(),
            PlanId = planId,
            CreatedByUserId = Guid.NewGuid(),
            BillingCycle = 1, // Monthly
            Amount = 29.99m,
            Currency = "USD",
            StartDate = (DateTime?)null,
            TrialDays = (int?)null
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateSubscription_ShouldReturnBadRequest_WithInvalidData()
    {
        // Arrange
        var request = new
        {
            TenantId = Guid.Empty, // Invalid
            PlanId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid(),
            BillingCycle = "Monthly",
            Amount = 29.99m,
            Currency = "USD",
            StartDate = DateTime.UtcNow,
            TrialDays = (int?)null
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSubscriptionById_ShouldReturn404_WhenNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/subscriptions/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

        [Fact]
    public async Task CreateAndGetSubscription_ShouldReturnCreatedSubscription()
    {
        // Arrange
        var planId = await SeedSubscriptionPlanAsync();
        var createRequest = new
        {
            TenantId = Guid.NewGuid(),
            PlanId = planId,
            CreatedByUserId = Guid.NewGuid(),
            BillingCycle = 1, // Monthly
            Amount = 29.99m,
            Currency = "USD",
            StartDate = (DateTime?)null,
            TrialDays = 14
        };

        // Act
        var createResponse = await _client.PostAsJsonAsync("/api/v1/subscriptions", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var responseContent = await createResponse.Content.ReadAsStringAsync();
        var subscription = System.Text.Json.JsonDocument.Parse(responseContent).RootElement;
        // Try both "id" and "Id" for case sensitivity
        var subscriptionId = subscription.TryGetProperty("id", out var idProp) ? idProp.GetGuid() : 
                           subscription.TryGetProperty("Id", out var idPropCap) ? idPropCap.GetGuid() : 
                           throw new Exception($"Could not find id property in response: {responseContent}");
        var getResponse = await _client.GetAsync($"/api/v1/subscriptions/{subscriptionId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CancelSubscription_ShouldReturn200_WhenSubscriptionExists()
    {
        // Arrange
        var planId = await SeedSubscriptionPlanAsync();
        
        // Create subscription
        var createRequest = new
        {
            TenantId = Guid.NewGuid(),
            PlanId = planId,
            CreatedByUserId = Guid.NewGuid(),
            BillingCycle = 1, // Monthly
            Amount = 29.99m,
            Currency = "USD",
            StartDate = (DateTime?)null,
            TrialDays = (int?)null
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/v1/subscriptions", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var responseContent = await createResponse.Content.ReadAsStringAsync();
        var subscription = System.Text.Json.JsonDocument.Parse(responseContent).RootElement;
        // Try both "id" and "Id" for case sensitivity
        var subscriptionId = subscription.TryGetProperty("id", out var idProp) ? idProp.GetGuid() : 
                           subscription.TryGetProperty("Id", out var idPropCap) ? idPropCap.GetGuid() : 
                           throw new Exception($"Could not find id property in response: {responseContent}");

        var cancelRequest = new
        {
            Reason = "UserRequested"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/subscriptions/{subscriptionId}:cancel", cancelRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
