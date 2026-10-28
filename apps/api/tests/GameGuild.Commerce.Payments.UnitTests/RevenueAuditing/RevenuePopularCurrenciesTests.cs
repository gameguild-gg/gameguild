using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.RevenueAuditing;

/// <summary>
///     Popular-currencies report (issue #412 spec G): ranking by transaction count, tie-break
///     by net volume then code, per-currency totals without cross-currency consolidation, and
///     the tenant-scope resolution shared with the other revenue report queries.
/// </summary>
public class RevenuePopularCurrenciesTests
{
    private readonly Mock<IRevenueEventRepository> _revenueEvents = new();
    private readonly Mock<IRevenueReconciliationRepository> _runs = new();
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();

    private RevenueReportService CreateService()
        => new(_revenueEvents.Object, _runs.Object);

    private void SetupDailyTotals(params RevenueDailyTotal[] totals)
    {
        _revenueEvents
            .Setup(repository => repository.GetDailyTotalsAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(totals.ToList());
    }

    private static RevenueDailyTotal Total(DateTime day, string currency, decimal credit, decimal debit, int count)
        => new(day, currency, credit, debit, credit - debit, count);

    [Fact]
    public async Task GetPopularCurrenciesAsync_RanksByTransactionCountThenNetThenCode()
    {
        var day = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        SetupDailyTotals(
            Total(day, "EUR", 300m, 0m, 30),   // rank 2: fewer transactions than USD
            Total(day.AddDays(1), "EUR", 100m, 20m, 10),
            Total(day, "USD", 900m, 100m, 40), // rank 1: most transactions
            Total(day, "GBP", 5m, 0m, 3));     // rank 3

        var report = await CreateService().GetPopularCurrenciesAsync(
            day, day.AddDays(2), tenantId: null, limit: 10);

        report.Currencies.Select(entry => entry.Currency).Should().Equal("USD", "EUR", "GBP");
        report.Currencies[0].Rank.Should().Be(1);
        report.Currencies[0].TransactionCount.Should().Be(40);
        report.Currencies[0].TotalCredit.Should().Be(900m);
        report.Currencies[0].TotalDebit.Should().Be(100m);
        report.Currencies[0].TotalNet.Should().Be(800m);

        // EUR aggregated across both days, never merged with USD.
        var eur = report.Currencies[1];
        eur.TransactionCount.Should().Be(40);
        eur.TotalCredit.Should().Be(400m);
        eur.TotalNet.Should().Be(280m);
    }

    [Fact]
    public async Task GetPopularCurrenciesAsync_CountTie_BreaksByNetVolumeThenCode()
    {
        var day = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        SetupDailyTotals(
            Total(day, "JPY", 5000m, 0m, 7),
            Total(day, "BRL", 100m, 0m, 7),
            Total(day, "CHF", 5000m, 0m, 7));

        var report = await CreateService().GetPopularCurrenciesAsync(day, day, tenantId: null, limit: 10);

        // JPY and CHF tie on count and |net| → code order; BRL ties on count with a smaller net → last.
        report.Currencies.Select(entry => entry.Currency).Should().Equal("CHF", "JPY", "BRL");
    }

    [Fact]
    public async Task GetPopularCurrenciesAsync_LimitsEntries()
    {
        var day = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        SetupDailyTotals(
            Total(day, "USD", 100m, 0m, 9),
            Total(day, "EUR", 100m, 0m, 8),
            Total(day, "GBP", 100m, 0m, 7));

        var report = await CreateService().GetPopularCurrenciesAsync(day, day, tenantId: null, limit: 2);

        report.Currencies.Should().HaveCount(2);
        report.Currencies.Select(entry => entry.Currency).Should().Equal("USD", "EUR");
    }

    [Fact]
    public async Task GetPopularCurrenciesAsync_NoEvents_ReturnsEmptyRanking()
    {
        SetupDailyTotals();

        var report = await CreateService().GetPopularCurrenciesAsync(
            new DateTime(2026, 10, 1), new DateTime(2026, 10, 2), tenantId: null);

        report.Currencies.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPopularCurrenciesQueryHandler_NonAdminActor_IsScopedToOwnTenant()
    {
        _revenueEvents
            .Setup(repository => repository.GetDailyTotalsAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var tenantId = Guid.NewGuid();
        _actorAccessor.SetupGet(accessor => accessor.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Test",
            IsAuthenticated = true
        });

        var handler = new Queries.RevenueAuditingReports.GetPopularCurrenciesQueryHandler(CreateService(), _actorAccessor.Object);
        await handler.Handle(
            new Queries.RevenueAuditingReports.GetPopularCurrenciesQuery(new DateTime(2026, 10, 1), new DateTime(2026, 10, 2), TenantId: Guid.NewGuid()),
            CancellationToken.None);

        _revenueEvents.Verify(repository => repository.GetDailyTotalsAsync(
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
