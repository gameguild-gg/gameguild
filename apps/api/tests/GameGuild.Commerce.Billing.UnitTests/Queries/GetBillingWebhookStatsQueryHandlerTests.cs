using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Queries;

/// <summary>
///     Issue #408 webhook activity dashboard: received/processed/failed counts, the success
///     rate, the average received-to-processed latency, dead-lettered and unverified counts,
///     and the per-provider breakdown over the reporting window.
/// </summary>
public class GetBillingWebhookStatsQueryHandlerTests : IAsyncDisposable
{
    private readonly BillingStatsTestDbContext _context = new(
        new DbContextOptionsBuilder<BillingStatsTestDbContext>()
            .UseInMemoryDatabase($"webhook-stats-{Guid.NewGuid():N}")
            .Options);

    [Fact]
    public async Task Handle_Should_Aggregate_Window_Totals_Rates_And_PerProvider()
    {
        var now = SystemClock.UtcNow;

        // Stripe: received 3h ago, processed 2 minutes later.
        AddEvent(PaymentProviders.Stripe, now.AddHours(-3), processedAt: now.AddHours(-3).AddMinutes(2));
        // Stripe: received 1h ago, failed, still retrying (not dead-lettered).
        AddEvent(PaymentProviders.Stripe, now.AddHours(-1), isFailed: true);
        // PayPal: received 2h ago, dead-lettered after exhausting retries.
        AddEvent(PaymentProviders.PayPal, now.AddHours(-2), deadLetteredAt: now.AddHours(-1));
        // PayPal: received 30 minutes ago, processed 30 seconds later, signature did not verify.
        AddEvent(
            PaymentProviders.PayPal,
            now.AddMinutes(-30),
            processedAt: now.AddMinutes(-30).AddSeconds(30),
            signatureVerified: false);
        // Stripe: old event outside the 24h default window — excluded from every aggregate.
        AddEvent(PaymentProviders.Stripe, now.AddDays(-3), processedAt: now.AddDays(-3).AddMinutes(1));
        await _context.SaveChangesAsync();

        var handler = new GetBillingWebhookStatsQueryHandler(_context);
        var stats = await handler.Handle(new GetBillingWebhookStatsQuery(), CancellationToken.None);

        stats.ReceivedInWindow.Should().Be(4);
        stats.ProcessedInWindow.Should().Be(2);
        stats.FailedPendingRetry.Should().Be(1);
        stats.DeadLetteredInWindow.Should().Be(1);
        stats.UnverifiedInWindow.Should().Be(1);
        stats.SuccessRatePercent.Should().Be(50.0);
        stats.AverageProcessingSeconds.Should().Be(75.0); // (120s + 30s) / 2

        stats.PerProvider.Should().HaveCount(2);

        var stripe = stats.PerProvider.Should().ContainSingle(item => item.Provider == PaymentProviders.Stripe).Subject;
        stripe.Received.Should().Be(2);
        stripe.Processed.Should().Be(1);
        stripe.FailedPendingRetry.Should().Be(1);
        stripe.DeadLettered.Should().Be(0);
        stripe.Unverified.Should().Be(0);

        var paypal = stats.PerProvider.Should().ContainSingle(item => item.Provider == PaymentProviders.PayPal).Subject;
        paypal.Received.Should().Be(2);
        paypal.Processed.Should().Be(1);
        paypal.FailedPendingRetry.Should().Be(0);
        paypal.DeadLettered.Should().Be(1);
        paypal.Unverified.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_Return_Null_Rates_For_An_Empty_Window()
    {
        var handler = new GetBillingWebhookStatsQueryHandler(_context);

        var stats = await handler.Handle(new GetBillingWebhookStatsQuery(), CancellationToken.None);

        stats.ReceivedInWindow.Should().Be(0);
        stats.ProcessedInWindow.Should().Be(0);
        stats.FailedPendingRetry.Should().Be(0);
        stats.DeadLetteredInWindow.Should().Be(0);
        stats.UnverifiedInWindow.Should().Be(0);
        stats.SuccessRatePercent.Should().BeNull();
        stats.AverageProcessingSeconds.Should().BeNull();
        stats.PerProvider.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Swap_A_Reversed_Window()
    {
        var now = SystemClock.UtcNow;
        AddEvent(PaymentProviders.Stripe, now.AddMinutes(-10));
        await _context.SaveChangesAsync();

        var handler = new GetBillingWebhookStatsQueryHandler(_context);

        var stats = await handler.Handle(
            new GetBillingWebhookStatsQuery(FromUtc: now, ToUtc: now.AddDays(-1)),
            CancellationToken.None);

        stats.FromUtc.Should().BeOnOrBefore(stats.ToUtc);
        stats.ReceivedInWindow.Should().Be(1);
    }

    private void AddEvent(
        string provider,
        DateTime receivedAt,
        DateTime? processedAt = null,
        bool isFailed = false,
        DateTime? deadLetteredAt = null,
        bool? signatureVerified = null)
    {
        _context.Set<BillingWebhookEvent>().Add(new BillingWebhookEvent
        {
            Provider = provider,
            ExternalEventId = $"evt_{Guid.NewGuid():N}",
            EventType = "invoice.payment_succeeded",
            Payload = "{}",
            CreatedAt = receivedAt,
            IsProcessed = processedAt is not null,
            ProcessedAt = processedAt,
            IsFailed = isFailed,
            DeadLetteredAt = deadLetteredAt,
            IsSignatureVerified = signatureVerified
        });
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private sealed class BillingStatsTestDbContext(DbContextOptions<BillingStatsTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BillingWebhookEvent>();
            base.OnModelCreating(modelBuilder);
        }
    }
}
