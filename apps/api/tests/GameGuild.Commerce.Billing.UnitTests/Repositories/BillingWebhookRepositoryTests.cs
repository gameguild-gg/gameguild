using FluentAssertions;

using GameGuild.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Repositories;

public class BillingWebhookRepositoryTests
{
    [Fact]
    public async Task CreateAsync_Should_Return_Existing_When_Duplicate()
    {
        await using var context = CreateContext();
        var repository = new BillingWebhookRepository(context, NullLogger<BillingWebhookRepository>.Instance);

        var existing = new BillingWebhookEvent
        {
            ExternalEventId = "evt",
            Provider = PaymentProviders.Stripe,
            EventType = "type",
            Payload = "{}"
        };
        context.BillingWebhookEvents.Add(existing);
        await context.SaveChangesAsync();

        var created = await repository.CreateAsync(new BillingWebhookEvent
        {
            ExternalEventId = "evt",
            Provider = PaymentProviders.Stripe,
            EventType = "type",
            Payload = "{}"
        });

        created.Id.Should().Be(existing.Id);
    }

    [Fact]
    public async Task GetFailedEventsAsync_Should_Filter_By_Attempts()
    {
        await using var context = CreateContext();
        var repository = new BillingWebhookRepository(context, NullLogger<BillingWebhookRepository>.Instance);

        context.BillingWebhookEvents.AddRange(
            new BillingWebhookEvent { ExternalEventId = "1", Provider = "stripe", EventType = "type", Payload = "{}", IsFailed = true, ProcessingAttempts = 1 },
            new BillingWebhookEvent { ExternalEventId = "2", Provider = "stripe", EventType = "type", Payload = "{}", IsFailed = true, ProcessingAttempts = 5 },
            new BillingWebhookEvent { ExternalEventId = "3", Provider = "stripe", EventType = "type", Payload = "{}", IsFailed = false, ProcessingAttempts = 0 });
        await context.SaveChangesAsync();

        var results = (await repository.GetFailedEventsAsync(3)).ToList();

        results.Should().HaveCount(1);
        results[0].ExternalEventId.Should().Be("1");
    }

    [Fact]
    public async Task GetByProviderScopeAsync_Should_Not_Collapse_Different_Accounts_Or_Endpoints()
    {
        await using var context = CreateContext();
        var repository = new BillingWebhookRepository(context, NullLogger<BillingWebhookRepository>.Instance);
        var accountEvent = CreateScopedEvent("acct_a", "we_a");
        var endpointEvent = CreateScopedEvent("acct_b", "we_b");
        context.BillingWebhookEvents.AddRange(accountEvent, endpointEvent);
        await context.SaveChangesAsync();

        var found = await repository.GetByProviderScopeAsync(
            PaymentProviders.Stripe,
            "live",
            "acct_b",
            "we_b",
            "evt_shared");

        found.Should().BeSameAs(endpointEvent);
        found.Should().NotBeSameAs(accountEvent);
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_True_For_Match()
    {
        await using var context = CreateContext();
        var repository = new BillingWebhookRepository(context, NullLogger<BillingWebhookRepository>.Instance);

        context.BillingWebhookEvents.Add(new BillingWebhookEvent
        {
            ExternalEventId = "evt",
            Provider = PaymentProviders.PayPal,
            EventType = "type",
            Payload = "{}"
        });
        await context.SaveChangesAsync();

        var exists = await repository.ExistsAsync("evt", PaymentProviders.PayPal);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Event_When_Found()
    {
        await using var context = CreateContext();
        var repository = new BillingWebhookRepository(context, NullLogger<BillingWebhookRepository>.Instance);

        var webhookEvent = new BillingWebhookEvent
        {
            ExternalEventId = "evt",
            Provider = PaymentProviders.Stripe,
            EventType = "type",
            Payload = "{}"
        };
        context.BillingWebhookEvents.Add(webhookEvent);
        await context.SaveChangesAsync();

        await repository.DeleteAsync(webhookEvent.Id);

        (await context.BillingWebhookEvents.CountAsync()).Should().Be(0);
    }

    private static TestBillingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestBillingDbContext>()
            .UseInMemoryDatabase($"Billing_{Guid.NewGuid()}")
            .Options;

        return new TestBillingDbContext(options);
    }

    private static BillingWebhookEvent CreateScopedEvent(string accountId, string endpointId) => new()
    {
        ExternalEventId = "evt_shared",
        Provider = PaymentProviders.Stripe,
        ProviderEnvironment = "live",
        ProviderAccountId = accountId,
        WebhookEndpointId = endpointId,
        EventType = "customer.created",
        Payload = "{}"
    };

    private sealed class TestBillingDbContext(DbContextOptions<TestBillingDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<BillingWebhookEvent> BillingWebhookEvents { get; set; } = null!;

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Mock.Of<IDbContextTransaction>());
        }
    }
}
