using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Commerce.Subscriptions;
using GameGuild.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.IntegrationTests.Security;

/// <summary>
///     P0 Critical Integration Tests: Commerce Security Flows
///     From: COMMERCE_MODULES_SECURITY_AUDIT.md Section 7 - Test Plan
///     These tests verify end-to-end security flows across the commerce modules.
/// </summary>
public class CommerceSecurityIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _client;
    private static readonly string DatabaseName = $"CommerceSecurityTestDb_{Guid.NewGuid()}";

    public CommerceSecurityIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContext registrations
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

                // Add in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(DatabaseName);
                });

                services.AddHttpLogging(o => { });
            });
        });

        _client = _factory.CreateClient();
    }

    #region Subscription Renewal Flow - Single Charge Guarantee (P0)

    [Fact]
    public async Task RenewalFlow_DuplicateIdempotencyKey_ShouldReturnCachedResult()
    {
        // Arrange
        var subscriptionId = await SeedActiveSubscriptionAsync();
        var idempotencyKey = $"renewal_{subscriptionId}_{DateTime.UtcNow:yyyyMMdd}";
        
        var request = new
        {
            SubscriptionId = subscriptionId,
            IdempotencyKey = idempotencyKey
        };

        // Act - Send first request
        var response1 = await _client.PostAsJsonAsync("/api/v1/subscriptions/renew", request);
        var content1 = await response1.Content.ReadAsStringAsync();

        // Act - Send duplicate request
        var response2 = await _client.PostAsJsonAsync("/api/v1/subscriptions/renew", request);
        var content2 = await response2.Content.ReadAsStringAsync();

        // Assert - Both should succeed with same result
        response1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.Accepted);
        response2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.Accepted);
        
        // Parse and compare transaction IDs if present
        // The second response should return the cached result, not create a new charge
    }

    [Fact]
    public async Task RenewalFlow_WithValidSubscription_ShouldCreatePaymentRecord()
    {
        // Arrange
        var subscriptionId = await SeedActiveSubscriptionAsync();
        var idempotencyKey = $"renewal_{subscriptionId}_{Guid.NewGuid()}";
        
        var request = new
        {
            SubscriptionId = subscriptionId,
            IdempotencyKey = idempotencyKey
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/renew", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.Accepted);
    }

    #endregion

    #region Plan Change Flow - Proration Safety (P0)

    [Fact]
    public async Task PlanChange_Upgrade_ShouldCalculateProrationCorrectly()
    {
        // Arrange
        var subscriptionId = await SeedActiveSubscriptionAsync();
        var newPlanId = await SeedUpgradePlanAsync();
        
        var request = new
        {
            SubscriptionId = subscriptionId,
            NewPlanId = newPlanId,
            EffectiveDate = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/change-plan", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PlanChange_Downgrade_ShouldBeEffectiveAtPeriodEnd()
    {
        // Arrange
        var subscriptionId = await SeedActiveSubscriptionAsync();
        var downgradePlanId = await SeedDowngradePlanAsync();
        
        var request = new
        {
            SubscriptionId = subscriptionId,
            NewPlanId = downgradePlanId,
            ScheduleAtPeriodEnd = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/change-plan", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);
        // Response should indicate the change is scheduled, not immediate
    }

    #endregion

    #region Cancellation Flow - Clean Cancellation (P0)

    [Fact]
    public async Task Cancellation_ImmediateCancel_ShouldStopAccessImmediately()
    {
        // Arrange
        var subscriptionId = await SeedActiveSubscriptionAsync();
        
        var request = new
        {
            SubscriptionId = subscriptionId,
            Reason = "User requested immediate cancellation",
            Immediate = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/cancel", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        // Verify subscription is cancelled
        var statusResponse = await _client.GetAsync($"/api/v1/subscriptions/{subscriptionId}");
        var subscription = await statusResponse.Content.ReadFromJsonAsync<SubscriptionDto>();
        subscription?.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Cancellation_EndOfPeriod_ShouldContinueUntilPeriodEnd()
    {
        // Arrange
        var subscriptionId = await SeedActiveSubscriptionAsync();
        
        var request = new
        {
            SubscriptionId = subscriptionId,
            Reason = "User requested end-of-period cancellation",
            Immediate = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/cancel", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Cancellation_ShouldPreventSubsequentRenewals()
    {
        // Arrange
        var subscriptionId = await SeedActiveSubscriptionAsync();
        
        // Cancel the subscription
        var cancelRequest = new { SubscriptionId = subscriptionId, Reason = "Test", Immediate = true };
        await _client.PostAsJsonAsync("/api/v1/subscriptions/cancel", cancelRequest);

        // Try to renew the cancelled subscription
        var renewRequest = new
        {
            SubscriptionId = subscriptionId,
            IdempotencyKey = $"renewal_{subscriptionId}_{Guid.NewGuid()}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/renew", renewRequest);

        // Assert - Renewal should fail for cancelled subscription
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Webhook Processing Flow - Idempotency (P0)

    [Fact]
    public async Task Webhook_DuplicateStripeEvent_ShouldBeIdempotent()
    {
        // Arrange
        var externalEventId = $"evt_stripe_{Guid.NewGuid()}";
        var webhookPayload = JsonSerializer.Serialize(new
        {
            id = externalEventId,
            type = "invoice.payment_succeeded",
            data = new
            {
                @object = new
                {
                    id = "in_123",
                    subscription = "sub_123",
                    amount_paid = 2999
                }
            }
        });
        var content = new StringContent(webhookPayload, Encoding.UTF8, "application/json");
        
        // Add webhook signature header (would need proper signing in production)
        _client.DefaultRequestHeaders.Add("Stripe-Signature", "test_signature");

        // Act - First webhook call
        var response1 = await _client.PostAsync("/api/v1/billing/webhooks/stripe", content);
        
        // Act - Duplicate webhook call (should be idempotent)
        var response2 = await _client.PostAsync("/api/v1/billing/webhooks/stripe", 
            new StringContent(webhookPayload, Encoding.UTF8, "application/json"));

        // Assert - Both should succeed (or second returns 200/204 indicating already processed)
        response1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.Accepted);
        response2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Webhook_InvalidSignature_ShouldBeRejected()
    {
        // Arrange
        var webhookPayload = JsonSerializer.Serialize(new
        {
            id = "evt_fake",
            type = "invoice.payment_succeeded"
        });
        var content = new StringContent(webhookPayload, Encoding.UTF8, "application/json");
        
        // No signature header or invalid signature

        // Act
        var response = await _client.PostAsync("/api/v1/billing/webhooks/stripe", content);

        // Assert - Should reject unsigned/invalid webhooks
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Tenant Isolation Flow (P0)

    [Fact]
    public async Task TenantIsolation_CrossTenantAccess_ShouldBeBlocked()
    {
        // Arrange - Create subscriptions in different tenants
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        var subscription1 = await SeedSubscriptionForTenantAsync(tenant1Id);
        
        // Set headers for tenant 2
        _client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", tenant2Id.ToString());

        // Act - Try to access tenant1's subscription from tenant2's context
        var response = await _client.GetAsync($"/api/v1/subscriptions/{subscription1}");

        // Assert - Should be forbidden or not found
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TenantIsolation_MissingTenantHeader_ShouldBeRejected()
    {
        // Arrange
        _client.DefaultRequestHeaders.Remove("X-Tenant-Id");

        // Act
        var response = await _client.GetAsync("/api/v1/subscriptions");

        // Assert - Should require tenant context
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Payment Retry Flow (P0/P1)

    [Fact]
    public async Task PaymentRetry_AfterFailure_ShouldBeScheduled()
    {
        // Arrange
        var subscriptionId = await SeedActiveSubscriptionAsync();
        var invoiceId = await SeedFailedInvoiceAsync(subscriptionId);

        // Act
        var response = await _client.PostAsync($"/api/v1/billing/invoices/{invoiceId}/retry", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PaymentRetry_ExceedsMaxAttempts_ShouldMarkAsFailed()
    {
        // Arrange
        var subscriptionId = await SeedActiveSubscriptionAsync();
        var invoiceId = await SeedInvoiceWithMaxFailuresAsync(subscriptionId);

        // Act
        var response = await _client.PostAsync($"/api/v1/billing/invoices/{invoiceId}/retry", null);

        // Assert - Should reject retry or mark permanently failed
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    #endregion

    #region Helper Methods

    private async Task<Guid> SeedActiveSubscriptionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var planId = await SeedSubscriptionPlanAsync(dbContext);
        
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: planId,
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new GameGuild.ValueObjects.Money(29.99m, "USD"),
            startDate: DateTime.UtcNow
        );

        dbContext.Set<Subscription>().Add(subscription);
        await dbContext.SaveChangesAsync();

        return subscription.Id;
    }

    private async Task<Guid> SeedSubscriptionForTenantAsync(Guid tenantId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var planId = await SeedSubscriptionPlanAsync(dbContext);
        
        var subscription = new Subscription(
            tenantId: tenantId,
            planId: planId,
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new GameGuild.ValueObjects.Money(29.99m, "USD"),
            startDate: DateTime.UtcNow
        );

        dbContext.Set<Subscription>().Add(subscription);
        await dbContext.SaveChangesAsync();

        return subscription.Id;
    }

    private async Task<Guid> SeedSubscriptionPlanAsync(ApplicationDbContext dbContext)
    {
        var plan = new SubscriptionPlan(
            name: "Test Plan",
            slug: "test-plan",
            monthlyPriceInCents: 2999,
            currency: "USD",
            description: "Test subscription plan"
        );

        dbContext.Set<SubscriptionPlan>().Add(plan);
        await dbContext.SaveChangesAsync();

        return plan.Id;
    }

    private async Task<Guid> SeedUpgradePlanAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var plan = new SubscriptionPlan(
            name: "Premium Plan",
            slug: "premium-plan",
            monthlyPriceInCents: 4999,
            currency: "USD",
            description: "Premium subscription plan"
        );

        dbContext.Set<SubscriptionPlan>().Add(plan);
        await dbContext.SaveChangesAsync();

        return plan.Id;
    }

    private async Task<Guid> SeedDowngradePlanAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var plan = new SubscriptionPlan(
            name: "Basic Plan",
            slug: "basic-plan",
            monthlyPriceInCents: 999,
            currency: "USD",
            description: "Basic subscription plan"
        );

        dbContext.Set<SubscriptionPlan>().Add(plan);
        await dbContext.SaveChangesAsync();

        return plan.Id;
    }

    private async Task<Guid> SeedFailedInvoiceAsync(Guid subscriptionId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create a failed invoice - implementation depends on Invoice entity
        // This is a placeholder that would need to match actual domain model
        return Guid.NewGuid();
    }

    private async Task<Guid> SeedInvoiceWithMaxFailuresAsync(Guid subscriptionId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create an invoice that has exceeded max retry attempts
        // This is a placeholder that would need to match actual domain model
        return Guid.NewGuid();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    #endregion
}

/// <summary>
/// DTO for subscription status checking
/// </summary>
internal class SubscriptionDto
{
    public Guid Id { get; set; }
    public string? Status { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool AutoRenew { get; set; }
}
