using System.Data.Common;

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
    public async Task CreateAsync_ShouldReloadWinner_WhenConcurrentInsertWinsUniqueConstraint()
    {
        await using var context = CreateRaceContext();
        var repository = new BillingWebhookRepository(context, NullLogger<BillingWebhookRepository>.Instance);
        var contender = CreateScopedEvent("acct_platform", "we_primary");
        context.ConcurrentWinner = CreateScopedEvent("acct_platform", "we_primary");
        context.SqlStateOnNextSave = "23505";
        context.ConstraintNameOnNextSave = "ix_billing_webhook_events_provider_scope_event";

        var created = await repository.CreateAsync(contender);

        created.Id.Should().Be(context.ConcurrentWinner.Id);
        created.Id.Should().NotBe(contender.Id);
        (await context.BillingWebhookEvents.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_ShouldRethrowNonUniqueDatabaseFailure_EvenWhenMatchingRowExists()
    {
        await using var context = CreateRaceContext();
        var repository = new BillingWebhookRepository(context, NullLogger<BillingWebhookRepository>.Instance);
        var contender = CreateScopedEvent("acct_platform", "we_primary");
        context.ConcurrentWinner = CreateScopedEvent("acct_platform", "we_primary");
        context.SqlStateOnNextSave = "23514";

        var action = () => repository.CreateAsync(contender);

        var exception = await action.Should().ThrowAsync<DbUpdateException>();
        exception.Which.InnerException.Should().BeOfType<SimulatedDbException>()
            .Which.SqlState.Should().Be("23514");
    }

    [Fact]
    public async Task CreateAsync_ShouldRethrowUniqueViolation_FromUnrelatedConstraint()
    {
        await using var context = CreateRaceContext();
        var repository = new BillingWebhookRepository(context, NullLogger<BillingWebhookRepository>.Instance);
        var contender = CreateScopedEvent("acct_platform", "we_primary");
        context.ConcurrentWinner = CreateScopedEvent("acct_platform", "we_primary");
        context.SqlStateOnNextSave = "23505";
        context.ConstraintNameOnNextSave = "ux_unrelated_table_key";

        var action = () => repository.CreateAsync(contender);

        var exception = await action.Should().ThrowAsync<DbUpdateException>();
        exception.Which.InnerException.Should().BeOfType<SimulatedDbException>()
            .Which.ConstraintName.Should().Be("ux_unrelated_table_key");
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

    [Fact]
    public async Task CreateAsync_Should_Stamp_The_Captured_Delivery_Source()
    {
        await using var context = CreateContext();
        var deliveryContext = new WebhookDeliveryContext();
        deliveryContext.Capture("stripe", "203.0.113.9", "Stripe/1.0 webhook-agent");
        var repository = new BillingWebhookRepository(context, NullLogger<BillingWebhookRepository>.Instance, deliveryContext);

        var created = await repository.CreateAsync(CreateScopedEvent("acct_a", "we_a"));

        created.SourceIpAddress.Should().Be("203.0.113.9");
        created.UserAgent.Should().Be("Stripe/1.0 webhook-agent");

        var persisted = await context.BillingWebhookEvents.SingleAsync();
        persisted.SourceIpAddress.Should().Be("203.0.113.9");
        persisted.UserAgent.Should().Be("Stripe/1.0 webhook-agent");
    }

    [Fact]
    public async Task CreateAsync_Should_Bound_The_Captured_User_Agent_To_The_Column_Length()
    {
        await using var context = CreateContext();
        var deliveryContext = new WebhookDeliveryContext();
        deliveryContext.Capture("stripe", "198.51.100.7", new string('u', 600));
        var repository = new BillingWebhookRepository(context, NullLogger<BillingWebhookRepository>.Instance, deliveryContext);

        var created = await repository.CreateAsync(CreateScopedEvent("acct_b", "we_b"));

        created.UserAgent.Should().HaveLength(512);
    }

    [Fact]
    public async Task CreateAsync_Should_Keep_The_Original_Source_When_The_Context_Is_Empty()
    {
        await using var context = CreateContext();
        var repository = new BillingWebhookRepository(context, NullLogger<BillingWebhookRepository>.Instance, new WebhookDeliveryContext());

        var created = await repository.CreateAsync(CreateScopedEvent("acct_c", "we_c"));

        created.SourceIpAddress.Should().BeNull();
        created.UserAgent.Should().BeNull();
    }

    private static TestBillingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestBillingDbContext>()
            .UseInMemoryDatabase($"Billing_{Guid.NewGuid()}")
            .Options;

        return new TestBillingDbContext(options);
    }

    private static RaceBillingDbContext CreateRaceContext()
    {
        var options = new DbContextOptionsBuilder<TestBillingDbContext>()
            .UseInMemoryDatabase($"BillingRace_{Guid.NewGuid()}")
            .Options;

        return new RaceBillingDbContext(options);
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

    private class TestBillingDbContext(DbContextOptions<TestBillingDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<BillingWebhookEvent> BillingWebhookEvents { get; set; } = null!;

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Mock.Of<IDbContextTransaction>());
        }
    }

    private sealed class RaceBillingDbContext(DbContextOptions<TestBillingDbContext> options)
        : TestBillingDbContext(options)
    {
        public BillingWebhookEvent ConcurrentWinner { get; set; } = null!;
        public string? SqlStateOnNextSave { get; set; }
        public string? ConstraintNameOnNextSave { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (SqlStateOnNextSave is null)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }

            var sqlState = SqlStateOnNextSave;
            SqlStateOnNextSave = null;
            var contender = ChangeTracker.Entries<BillingWebhookEvent>()
                .Single(entry => entry.State == EntityState.Added);
            contender.State = EntityState.Detached;
            BillingWebhookEvents.Add(ConcurrentWinner);
            await base.SaveChangesAsync(cancellationToken);
            contender.State = EntityState.Added;

            throw new DbUpdateException(
                "Simulated database failure.",
                new SimulatedDbException(sqlState, ConstraintNameOnNextSave));
        }
    }

    private sealed class SimulatedDbException(
        string sqlState,
        string? constraintName) : DbException("Simulated PostgreSQL failure.")
    {
        public override string? SqlState => sqlState;
        public string? ConstraintName { get; } = constraintName;
    }
}
