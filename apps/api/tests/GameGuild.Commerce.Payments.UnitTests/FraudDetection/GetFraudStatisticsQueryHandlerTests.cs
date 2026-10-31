using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.FraudDetection;

/// <summary>
///     Issue #409 fraud-program statistics: totals, rates (flagged, blocked, chargeback,
///     false-positive proxy), the manual-review backlog with average time-to-review and the
///     top fraud signals parsed from the persisted evidence bundles.
/// </summary>
public class GetFraudStatisticsQueryHandlerTests : IAsyncDisposable
{
    private readonly PaymentsPersistenceTestDbContext _context = new(
        new DbContextOptionsBuilder<PaymentsPersistenceTestDbContext>()
            .UseInMemoryDatabase($"fraud-statistics-{Guid.NewGuid():N}")
            .Options);

    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Handle_Should_Aggregate_Totals_Rates_Reviews_And_TopSignals()
    {
        var now = SystemClock.UtcNow;

        // Allowed and succeeded.
        await AddPaymentAsync(
            _tenantId,
            payment =>
            {
                payment.MarkAsProcessing();
                payment.RecordFraudAssessment(
                    FraudDecision.Allow, 5, "low risk",
                    """{"signals":[{"type":"Velocity.PaymentFrequency"}]}""");
                payment.MarkAsSucceeded($"ext_{Guid.NewGuid():N}");
            });

        // Held for review, still pending (the live backlog).
        await AddPaymentAsync(
            _tenantId,
            payment =>
            {
                payment.MarkAsProcessing();
                payment.RecordFraudAssessment(
                    FraudDecision.Review, 55, "held for review",
                    """{"signals":[{"type":"Velocity.PaymentFrequency"}]}""");
            },
            createdAt: now.AddMinutes(-10));

        // Held for review, then approved (the false-positive proxy) two hours later.
        await AddPaymentAsync(
            _tenantId,
            payment =>
            {
                payment.MarkAsProcessing();
                payment.RecordFraudAssessment(FraudDecision.Review, 60, "held for review", null);
                payment.ResolveFraudReview(approved: true, Guid.NewGuid(), "legitimate");
            },
            createdAt: now.AddHours(-2));

        // Blocked by the gate.
        await AddPaymentAsync(
            _tenantId,
            payment =>
            {
                payment.MarkAsProcessing();
                payment.RecordFraudAssessment(
                    FraudDecision.Block, 95, "blocked",
                    """{"signals":[{"type":"Blacklist.CardFingerprint"},{"type":"AccountAge.NewAccount"}]}""");
            });

        // Allowed, succeeded, then disputed (chargeback).
        await AddPaymentAsync(
            _tenantId,
            payment =>
            {
                payment.MarkAsProcessing();
                payment.RecordFraudAssessment(FraudDecision.Allow, 10, "low risk", null);
                payment.MarkAsSucceeded($"ext_{Guid.NewGuid():N}");
                payment.MarkAsDisputed();
            });

        var handler = new GetFraudStatisticsQueryHandler(_context);
        var report = await handler.Handle(new GetFraudStatisticsQuery(), CancellationToken.None);

        report.Totals.PaymentsCreated.Should().Be(5);
        report.Totals.Assessed.Should().Be(5);
        report.Totals.Allowed.Should().Be(2);
        report.Totals.ReviewHeld.Should().Be(2);
        report.Totals.Blocked.Should().Be(1);
        report.Totals.Succeeded.Should().Be(1);
        report.Totals.Disputed.Should().Be(1);

        report.Rates.FlaggedPercent.Should().Be(60.0);
        report.Rates.BlockedPercent.Should().Be(20.0);
        report.Rates.ChargebackPercent.Should().Be(100.0);
        report.Rates.FalsePositivePercent.Should().Be(100.0);

        report.Reviews.PendingBacklog.Should().Be(1);
        report.Reviews.ResolvedInWindow.Should().Be(1);
        report.Reviews.ApprovedInWindow.Should().Be(1);
        report.Reviews.DeclinedInWindow.Should().Be(0);
        report.Reviews.AverageReviewHours.Should().BeInRange(1.9, 2.1);

        report.TopSignals.Should().BeEquivalentTo(new[]
        {
            new FraudStatisticsSignal { SignalType = "Velocity.PaymentFrequency", Count = 2 },
            new FraudStatisticsSignal { SignalType = "AccountAge.NewAccount", Count = 1 },
            new FraudStatisticsSignal { SignalType = "Blacklist.CardFingerprint", Count = 1 }
        });
    }

    [Fact]
    public async Task Handle_Should_Scope_To_Tenant_And_Window()
    {
        var now = SystemClock.UtcNow;
        var otherTenantId = Guid.NewGuid();

        await AddPaymentAsync(_tenantId, _ => { }, createdAt: now.AddMinutes(-5));
        await AddPaymentAsync(_tenantId, _ => { }, createdAt: now.AddMinutes(-1));
        await AddPaymentAsync(otherTenantId, _ => { }, createdAt: now.AddMinutes(-2));
        await AddPaymentAsync(_tenantId, _ => { }, createdAt: now.AddDays(-60)); // outside the 30d default window

        var handler = new GetFraudStatisticsQueryHandler(_context);

        var tenantReport = await handler.Handle(
            new GetFraudStatisticsQuery(TenantId: _tenantId), CancellationToken.None);
        tenantReport.Totals.PaymentsCreated.Should().Be(2);

        var allTenantsReport = await handler.Handle(
            new GetFraudStatisticsQuery(), CancellationToken.None);
        allTenantsReport.Totals.PaymentsCreated.Should().Be(3);

        var futureReport = await handler.Handle(
            new GetFraudStatisticsQuery(FromUtc: now.AddDays(1), ToUtc: now.AddDays(2)),
            CancellationToken.None);
        futureReport.Totals.PaymentsCreated.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_Return_Null_Rates_On_An_Empty_Window()
    {
        var handler = new GetFraudStatisticsQueryHandler(_context);

        var report = await handler.Handle(new GetFraudStatisticsQuery(), CancellationToken.None);

        report.Totals.PaymentsCreated.Should().Be(0);
        report.Rates.FlaggedPercent.Should().BeNull();
        report.Rates.BlockedPercent.Should().BeNull();
        report.Rates.ChargebackPercent.Should().BeNull();
        report.Rates.FalsePositivePercent.Should().BeNull();
        report.Reviews.PendingBacklog.Should().Be(0);
        report.Reviews.AverageReviewHours.Should().BeNull();
        report.TopSignals.Should().BeEmpty();
        report.FromUtc.Should().BeOnOrBefore(report.ToUtc);
    }

    [Fact]
    public async Task Handle_Should_Break_Down_By_Country_Provider_And_Day()
    {
        var now = SystemClock.UtcNow;

        // Blocked attempt from a high-risk country through stripe, yesterday.
        await AddPaymentAsync(
            _tenantId,
            payment =>
            {
                payment.RecordBillingCountry("NG");
                payment.MarkAsProcessing();
                payment.RecordFraudAssessment(FraudDecision.Block, 95, "blocked", null);
            },
            createdAt: now.AddDays(-1));

        // Held attempt from Portugal through paypal, today.
        await AddPaymentAsync(
            _tenantId,
            payment =>
            {
                payment.RecordBillingCountry("PT");
                payment.MarkAsProcessing();
                payment.RecordFraudAssessment(FraudDecision.Review, 55, "held", null);
            },
            createdAt: now.AddHours(-1),
            provider: "paypal");

        // Attempt without a recorded country through stripe, today.
        await AddPaymentAsync(_tenantId, payment => payment.MarkAsProcessing(), createdAt: now.AddHours(-2));

        var handler = new GetFraudStatisticsQueryHandler(_context);
        var report = await handler.Handle(new GetFraudStatisticsQuery(), CancellationToken.None);

        report.ByCountry.Should().BeEquivalentTo(new[]
        {
            new FraudStatisticsCountryRow { Country = "NG", Created = 1, ReviewHeld = 0, Blocked = 1 },
            new FraudStatisticsCountryRow { Country = "PT", Created = 1, ReviewHeld = 1, Blocked = 0 },
            new FraudStatisticsCountryRow { Country = "unknown", Created = 1, ReviewHeld = 0, Blocked = 0 }
        });

        report.ByPaymentMethod.Should().HaveCount(2);
        report.ByPaymentMethod.Should().Contain(row => row.Provider == "stripe" && row.Created == 2);
        report.ByPaymentMethod.Should().Contain(row => row.Provider == "paypal" && row.Created == 1 && row.ReviewHeld == 1);

        report.DailyTrend.Should().HaveCount(2);
        report.DailyTrend.Should().BeInAscendingOrder(point => point.Day);
        report.DailyTrend.Should().Contain(point => point.Created == 1 && point.Blocked == 1);
        report.DailyTrend.Should().Contain(point => point.Created == 2 && point.ReviewHeld == 1);
    }

    [Fact]
    public void FraudStatisticsCsvComposer_Should_Export_All_Sections()
    {
        var report = new FraudStatisticsReport
        {
            GeneratedAtUtc = SystemClock.UtcNow,
            FromUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            ToUtc = new DateTime(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc),
            Totals = new FraudStatisticsTotals
            {
                PaymentsCreated = 10, Assessed = 9, Allowed = 5, ReviewHeld = 3, Blocked = 1, Succeeded = 6, Disputed = 1
            },
            Rates = new FraudStatisticsRates
            {
                FlaggedPercent = 44.44, BlockedPercent = 11.11, ChargebackPercent = 16.67, FalsePositivePercent = 50.0
            },
            Reviews = new FraudStatisticsReviews
            {
                PendingBacklog = 2, ResolvedInWindow = 2, ApprovedInWindow = 1, DeclinedInWindow = 1, AverageReviewHours = 3.5
            },
            TopSignals =
            [
                new FraudStatisticsSignal { SignalType = "Velocity.PaymentFrequency", Count = 4 }
            ],
            ByCountry =
            [
                new FraudStatisticsCountryRow { Country = "NG", Created = 2, ReviewHeld = 1, Blocked = 1 }
            ],
            ByPaymentMethod =
            [
                new FraudStatisticsMethodRow { Provider = "stripe", Created = 9, ReviewHeld = 3, Blocked = 1 }
            ],
            DailyTrend =
            [
                new FraudStatisticsTrendPoint { Day = new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc), Created = 5, ReviewHeld = 2, Blocked = 0 }
            ]
        };

        var csv = System.Text.Encoding.UTF8.GetString(FraudStatisticsCsvComposer.Compose(report));

        csv.Should().Contain("summary,totals,10,3,1");
        csv.Should().Contain("summary,flagged_percent,44.44");
        csv.Should().Contain("by_country,NG,2,1,1");
        csv.Should().Contain("by_payment_method,stripe,9,3,1");
        csv.Should().Contain("daily_trend,2026-10-28,5,2,0");
        csv.Split('\n').First().Should().Be("section,key,created,review_held,blocked");
    }

    private async Task AddPaymentAsync(
        Guid tenantId,
        Action<Payment> mutate,
        DateTime? createdAt = null,
        string provider = "stripe")
    {
        var payment = Payment.Create(
            tenantId,
            25m,
            "USD",
            $"stats-{Guid.NewGuid():N}",
            provider: provider,
            subscriptionId: Guid.NewGuid(),
            externalCustomerId: $"cus_{Guid.NewGuid():N}",
            paymentMethodId: "pm_stats");

        mutate(payment);

        if (createdAt.HasValue)
        {
            payment.CreatedAt = createdAt.Value;
        }

        _context.Set<Payment>().Add(payment);
        await _context.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
