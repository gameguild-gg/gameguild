using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.HealthChecks;
using GameGuild.Commerce.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GameGuild.API.IntegrationTests.HealthChecks;

public sealed class BillingInboxReadinessHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenInboxHasOnlyFreshPendingWork_ReturnsHealthy()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Set<BillingWebhookEvent>().AddRange(
            CreateEvent(isProcessed: true, isFailed: false),
            CreateEvent(isProcessed: false, isFailed: false));
        await dbContext.SaveChangesAsync();
        var healthCheck = CreateHealthCheck(dbContext);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().Contain("pendingEvents", 1);
        result.Data.Should().Contain("staleEvents", 0);
        result.Data.Should().Contain("failedEvents", 0);
        result.Data.Should().Contain("exhaustedEvents", 0);
        result.Data.Should().Contain("legacyEvents", 0);
        result.Data["oldestPendingAgeSeconds"].Should().BeOfType<long>()
            .Which.Should().BeGreaterThanOrEqualTo(0);
        AssertSanitized(result, "sensitive-provider", "evt_sensitive", "payload-sensitive", "error-sensitive");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenInboxHasNoPendingWork_ReportsZeroOldestPendingAge()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Set<BillingWebhookEvent>().Add(CreateEvent(isProcessed: true, isFailed: false));
        await dbContext.SaveChangesAsync();
        var healthCheck = CreateHealthCheck(dbContext);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().Contain("oldestPendingAgeSeconds", 0L);
        AssertSanitized(result, "sensitive-provider", "evt_sensitive", "payload-sensitive", "error-sensitive");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenInboxRequiresAttention_ReturnsAggregateSanitizedDegradedResult()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Set<BillingWebhookEvent>().AddRange(
            CreateEvent(isProcessed: false, isFailed: false),
            CreateEvent(
                isProcessed: false,
                isFailed: false,
                createdAt: DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5))),
            CreateEvent(isProcessed: false, isFailed: true),
            CreateEvent(isProcessed: false, isFailed: true, processingAttempts: 3),
            CreateEvent(isProcessed: true, isFailed: false, providerEnvironment: null));
        await dbContext.SaveChangesAsync();
        var healthCheck = CreateHealthCheck(dbContext);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("Billing webhook inbox requires attention.");
        result.Data.Should().Contain("pendingEvents", 2);
        result.Data.Should().Contain("staleEvents", 1);
        result.Data.Should().Contain("failedEvents", 2);
        result.Data.Should().Contain("exhaustedEvents", 1);
        result.Data.Should().Contain("legacyEvents", 1);
        result.Data.Keys.Should().BeEquivalentTo(
            "pendingEvents",
            "staleEvents",
            "failedEvents",
            "exhaustedEvents",
            "legacyEvents",
            "oldestPendingAgeSeconds");
        result.Data["oldestPendingAgeSeconds"].Should().BeOfType<long>()
            .Which.Should().BeInRange(299L, 360L);
        AssertSanitized(result, "sensitive-provider", "evt_sensitive", "payload-sensitive", "error-sensitive");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenInboxQueryFails_ReturnsSanitizedUnhealthyResult()
    {
        var dbContext = CreateDbContext();
        var healthCheck = CreateHealthCheck(dbContext);
        await dbContext.DisposeAsync();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Billing webhook inbox readiness check failed.");
        result.Data.Should().BeEmpty();
        AssertSanitized(result, nameof(ObjectDisposedException), "ApplicationDbContext");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenBillingOptionsAccessFails_ReturnsSanitizedUnhealthyResult()
    {
        const string sensitiveFailure = "billing options failure containing payload-sensitive";
        await using var dbContext = CreateDbContext();
        var healthCheck = new BillingInboxReadinessHealthCheck(
            dbContext,
            new ThrowingOptions<BillingConfiguration>(sensitiveFailure));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Billing webhook inbox readiness check failed.");
        result.Data.Should().BeEmpty();
        AssertSanitized(result, sensitiveFailure);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"BillingInboxReadiness_{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static BillingInboxReadinessHealthCheck CreateHealthCheck(ApplicationDbContext dbContext) =>
        new(dbContext, Options.Create(CreateBillingConfiguration()));

    private static BillingConfiguration CreateBillingConfiguration() => new()
    {
        Webhook = new WebhookSettings
        {
            MaxRetryAttempts = 3,
            ProcessingTimeoutSeconds = 30
        }
    };

    private static BillingWebhookEvent CreateEvent(
        bool isProcessed,
        bool isFailed,
        int processingAttempts = 1,
        DateTime? createdAt = null,
        string? providerEnvironment = "live") => new()
    {
        Provider = "sensitive-provider",
        ExternalEventId = $"evt_sensitive_{Guid.NewGuid()}",
        ProviderEnvironment = providerEnvironment,
        EventType = "payment.sensitive",
        Payload = "payload-sensitive",
        ErrorMessage = "error-sensitive",
        IsProcessed = isProcessed,
        IsFailed = isFailed,
        ProcessingAttempts = processingAttempts,
        CreatedAt = createdAt ?? DateTime.UtcNow,
        ProcessedAt = isProcessed ? DateTime.UtcNow : null
    };

    private static void AssertSanitized(HealthCheckResult result, params string[] sensitiveValues)
    {
        result.Exception.Should().BeNull();

        var exposedOutput = string.Join(
            "|",
            result.Data.SelectMany(entry => new[] { entry.Key, entry.Value?.ToString() ?? string.Empty })
                .Prepend(result.Description ?? string.Empty));

        foreach (var sensitiveValue in sensitiveValues)
        {
            exposedOutput.Should().NotContain(sensitiveValue);
        }
    }

    private sealed class ThrowingOptions<T>(string message) : IOptions<T>
        where T : class
    {
        public T Value => throw new InvalidOperationException(message);
    }
}
