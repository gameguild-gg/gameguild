using System.Diagnostics;
using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using static GameGuild.Commerce.Payments.CurrencyConfigurationCommandHelpers;

namespace GameGuild.Commerce.Payments.UnitTests.RevenueAuditing;

/// <summary>
///     Base-currency reporting and conversion-cost analysis (issue #412 specs F/G), the FX
///     rate variance report, the via-base historical conversion composition (spec D) and the
///     cached-conversion performance budget (&lt; 50 ms).
/// </summary>
public class BaseCurrencyReportTests
{
    private readonly Mock<IRevenueEventRepository> _revenueEvents = new();
    private readonly Mock<IRevenueReconciliationRepository> _runs = new();
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    public BaseCurrencyReportTests()
    {
        _actorAccessor.SetupGet(accessor => accessor.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = _tenantId,
            Roles = new HashSet<string> { "TenantAdmin" },
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Test",
            IsAuthenticated = true,
        });
    }

    // ─────────────────── Base-currency revenue report + reconciliation ───────────────────

    [Fact]
    public async Task BaseCurrencyReport_ConvertsPerCurrencyAtPeriodEndRate_AndReconcilesTotals()
    {
        var db = NewDatabase();
        await SeedBaseAsync(db, baseCurrency: "USD");
        var periodEnd = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
        db.Set<FxRateSnapshot>().Add(new FxRateSnapshot
        {
            BaseCurrency = "USD", QuoteCurrency = "EUR", Rate = 0.9m,
            EffectiveDateUtc = periodEnd.Date, AsOfUtc = periodEnd,
            ProviderName = "openexchangerates", Source = "latest",
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var day = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        SetupDailyTotals(
            Total(day, "USD", 1000m, 100m, 20),
            Total(day, "EUR", 900m, 0m, 10));

        var report = await service.GetBaseCurrencyRevenueReportAsync(day, periodEnd, _tenantId);

        report.BaseCurrency.Should().Be("USD");
        var usd = report.Entries.Single(entry => entry.Currency == "USD");
        usd.Converted.Should().BeTrue();
        usd.RateUsed.Should().Be(1m);
        usd.BaseNetTotal.Should().Be(900m);

        var eur = report.Entries.Single(entry => entry.Currency == "EUR");
        eur.RateUsed.Should().Be(0.9m);
        eur.BaseNetTotal.Should().Be(1000.00m); // 900 EUR ÷ 0.9

        // Reconciliation: base grand total is the sum of converted rows.
        report.BaseNetTotal.Should().Be(1900.00m);
        report.UnconvertedCurrencies.Should().Be(0);
        report.Attestation.Should().Contain("EUR@0.9").And.Contain("1900.00 USD");
    }

    [Fact]
    public async Task BaseCurrencyReport_NoRateAtPeriodEnd_FailsClosedPerCurrency()
    {
        var db = NewDatabase();
        await SeedBaseAsync(db, baseCurrency: "USD");
        var service = CreateService(db);
        var day = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        SetupDailyTotals(
            Total(day, "USD", 100m, 0m, 5),
            Total(day, "BRL", 500m, 0m, 3)); // no USD/BRL quotation anywhere

        var report = await service.GetBaseCurrencyRevenueReportAsync(day, day.AddDays(1), _tenantId);

        var brl = report.Entries.Single(entry => entry.Currency == "BRL");
        brl.Converted.Should().BeFalse();
        brl.Note.Should().Be("no-rate-at-period-end");
        brl.BaseNetTotal.Should().BeNull();
        report.UnconvertedCurrencies.Should().Be(1);
        report.BaseNetTotal.Should().Be(100m); // BRL excluded, never merged
    }

    [Fact]
    public async Task BaseCurrencyReport_WithoutTenantBaseCurrency_FailsClosed()
    {
        var db = NewDatabase();
        var service = CreateService(db);
        var day = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        var act = () => service.GetBaseCurrencyRevenueReportAsync(day, day.AddDays(1), _tenantId);

        (await act.Should().ThrowAsync<BusinessRuleViolationException>())
            .Which.Rule.Should().Be("NoBaseCurrencyConfigured");

        var actUnscoped = () => service.GetBaseCurrencyRevenueReportAsync(day, day.AddDays(1), null);
        (await actUnscoped.Should().ThrowAsync<BusinessRuleViolationException>())
            .Which.Rule.Should().Be("BaseCurrencyReportRequiresTenantScope");
    }

    [Fact]
    public async Task BaseCurrencyReport_UsesInverseQuotation_WhenOnlyQuoteBaseExists()
    {
        var db = NewDatabase();
        await SeedBaseAsync(db, baseCurrency: "USD");
        var periodEnd = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
        db.Set<FxRateSnapshot>().Add(new FxRateSnapshot
        {
            BaseCurrency = "GBP", QuoteCurrency = "USD", Rate = 1.25m, // inverted: USD→GBP = 0.8
            EffectiveDateUtc = periodEnd.Date, AsOfUtc = periodEnd,
            ProviderName = "ecb", Source = "latest",
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var day = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        SetupDailyTotals(Total(day, "GBP", 80m, 0m, 4));

        var report = await service.GetBaseCurrencyRevenueReportAsync(day, periodEnd, _tenantId);

        var gbp = report.Entries.Single();
        gbp.RateUsed.Should().BeApproximately(0.8m, 0.000000000001m);
        gbp.BaseNetTotal.Should().Be(100m); // 80 GBP ÷ 0.8
        report.BaseNetTotal.Should().Be(100m);
    }

    [Fact]
    public async Task BaseCurrencyReport_Export_ProducesCsvWithBaseColumns()
    {
        var db = NewDatabase();
        await SeedBaseAsync(db, baseCurrency: "USD");
        var periodEnd = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
        db.Set<FxRateSnapshot>().Add(new FxRateSnapshot
        {
            BaseCurrency = "USD", QuoteCurrency = "EUR", Rate = 0.9m,
            EffectiveDateUtc = periodEnd.Date, AsOfUtc = periodEnd,
            ProviderName = "openexchangerates", Source = "latest",
        });
        await db.SaveChangesAsync();
        var day = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        SetupDailyTotals(
            Total(day, "USD", 100m, 0m, 2),
            Total(day, "EUR", 90m, 0m, 1));

        var export = await CreateService(db).ExportBaseCurrencyReportAsync(day, periodEnd, _tenantId);

        export.FileName.Should().Contain("revenue-base-currency");
        export.ContentType.Should().Be("text/csv; charset=utf-8");
        export.Content.Should().Contain("currency,credit_total,debit_total,net_total,event_count,converted,rate_used");
        export.Content.Should().Contain("EUR,90.00,0.00,90.00,1,true,0.9");
        export.Content.Should().Contain("total_USD");
    }

    // ─────────────────── Conversion-cost analysis ───────────────────

    [Fact]
    public async Task ConversionCostReport_QuantifiesOverrideVsMarketDelta()
    {
        var db = NewDatabase();
        await SeedBaseAsync(db, baseCurrency: "USD");
        var periodEnd = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
        db.Set<FxRateSnapshot>().AddRange(
            new FxRateSnapshot
            {
                BaseCurrency = "USD", QuoteCurrency = "EUR", Rate = 0.9m,
                EffectiveDateUtc = periodEnd.Date, AsOfUtc = periodEnd,
                ProviderName = "openexchangerates", Source = "latest",
            },
            new FxRateSnapshot
            {
                BaseCurrency = "USD", QuoteCurrency = "EUR", Rate = 0.8m, // operator pin
                EffectiveDateUtc = periodEnd.Date, AsOfUtc = periodEnd.AddHours(1),
                ProviderName = "manual", Source = "manual-override", IsOverride = true,
            });
        await db.SaveChangesAsync();
        var day = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        SetupDailyTotals(Total(day, "EUR", 900m, 0m, 9)); // net 900 EUR

        var report = await CreateService(db).GetConversionCostReportAsync(day, periodEnd, _tenantId);

        var eur = report.Entries.Single();
        eur.MarketRate.Should().Be(0.9m);
        eur.OverrideRate.Should().Be(0.8m);
        eur.BaseAtMarketRate.Should().Be(1000.00m); // 900 / 0.9
        eur.BaseAtOverrideRate.Should().Be(1125.00m); // 900 / 0.8
        eur.CostInBase.Should().Be(125.00m);
        report.TotalCostInBase.Should().Be(125.00m);
    }

    // ─────────────────── FX rate variance report ───────────────────

    [Fact]
    public async Task VarianceReport_FlagsOverridesBeyondThreshold()
    {
        var db = NewDatabase();
        var now = SystemClock.UtcNow;
        db.Set<FxRateSnapshot>().AddRange(
            new FxRateSnapshot
            {
                BaseCurrency = "USD", QuoteCurrency = "EUR", Rate = 0.9m,
                EffectiveDateUtc = now.Date, AsOfUtc = now.AddHours(-2),
                ProviderName = "openexchangerates", Source = "latest",
            },
            new FxRateSnapshot
            {
                BaseCurrency = "USD", QuoteCurrency = "EUR", Rate = 0.7m, // |0.7−0.9|/0.9 = 22% → flagged
                EffectiveDateUtc = now.Date, AsOfUtc = now.AddHours(-1),
                ProviderName = "manual", Source = "manual-override", IsOverride = true,
            },
            new FxRateSnapshot
            {
                BaseCurrency = "USD", QuoteCurrency = "JPY", Rate = 150m,
                EffectiveDateUtc = now.Date, AsOfUtc = now.AddHours(-2),
                ProviderName = "openexchangerates", Source = "latest",
            },
            new FxRateSnapshot
            {
                BaseCurrency = "USD", QuoteCurrency = "JPY", Rate = 152m, // 1.3% → within 10%
                EffectiveDateUtc = now.Date, AsOfUtc = now.AddHours(-1),
                ProviderName = "manual", Source = "manual-override", IsOverride = true,
            },
            new FxRateSnapshot
            {
                BaseCurrency = "USD", QuoteCurrency = "GBP", Rate = 0.75m, // no market rate for the pair
                EffectiveDateUtc = now.Date, AsOfUtc = now,
                ProviderName = "manual", Source = "manual-override", IsOverride = true,
            });
        await db.SaveChangesAsync();
        var handler = new GetFxRateVarianceReportQueryHandler(
            db, Microsoft.Extensions.Options.Options.Create(new FxSyncOptions()), SysAdminAccessor());

        var report = await handler.Handle(new GetFxRateVarianceReportQuery(), CancellationToken.None);

        report.ThresholdPercent.Should().Be(10m);
        report.Entries.Should().HaveCount(3);
        var eur = report.Entries.Single(entry => entry.QuoteCurrency == "EUR");
        eur.VariancePercent.Should().BeApproximately(22.2222m, 0.0001m);
        eur.ExceedsThreshold.Should().BeTrue();
        var jpy = report.Entries.Single(entry => entry.QuoteCurrency == "JPY");
        jpy.ExceedsThreshold.Should().BeFalse();
        var gbp = report.Entries.Single(entry => entry.QuoteCurrency == "GBP");
        gbp.VariancePercent.Should().BeNull();
        gbp.MarketRate.Should().BeNull();
    }

    // ─────────────────── Historical via-base composition (spec D) ───────────────────

    [Fact]
    public async Task HistoricalConversion_ComposesThroughGridBase_WhenNoDirectPairExists()
    {
        var db = NewDatabase();
        var tenantId = Guid.NewGuid();
        db.Set<CurrencyConfiguration>().AddRange(
            new CurrencyConfiguration { TenantId = tenantId, CurrencyCode = "USD", IsEnabled = true, IsBaseCurrency = true },
            new CurrencyConfiguration { TenantId = tenantId, CurrencyCode = "EUR", IsEnabled = true },
            new CurrencyConfiguration { TenantId = tenantId, CurrencyCode = "JPY", IsEnabled = true });
        var date = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        db.Set<FxRateSnapshot>().AddRange(
            new FxRateSnapshot
            {
                BaseCurrency = "USD", QuoteCurrency = "EUR", Rate = 0.8m,
                EffectiveDateUtc = date, AsOfUtc = date.AddHours(1),
                ProviderName = "openexchangerates", Source = "latest",
            },
            new FxRateSnapshot
            {
                BaseCurrency = "USD", QuoteCurrency = "JPY", Rate = 160m,
                EffectiveDateUtc = date, AsOfUtc = date.AddHours(1),
                ProviderName = "openexchangerates", Source = "latest",
            });
        await db.SaveChangesAsync();

        var handler = new ConvertCurrencyCommandHandler(
            db,
            new StubRateProvider(),
            TenantAccessor(tenantId));

        var result = await handler.Handle(
            new ConvertCurrencyCommand(
                "JPY",
                [new CurrencyConversionItemInput(1600m, "EUR")],
                AsOfUtc: date.AddDays(1)),
            CancellationToken.None);

        var item = result.Items.Single();
        item.Error.Should().BeNull();
        item.ConvertedAmount.Should().Be(320000m); // 1600 EUR × (160/0.8) via the USD grid
        item.RateSource.Should().Contain("composed via USD");
    }

    // ─────────────────── Cached conversion performance (< 50 ms, spec D) ───────────────────

    [Fact]
    public async Task CachedConversion_CompletesWithin50MsPerCall()
    {
        var source = new FxRuntimeRateSource(
            new EmptyScopeFactory(), NullLogger<FxRuntimeRateSource>.Instance);

        // A 40-quote USD grid applied directly (no database: the runtime source serves from
        // memory once seeded by a local mutation, which is exactly the cached path budgeted).
        source.ApplyProviderSet(new FxRateSet(
            "openexchangerates", "USD",
            BuildPerfGrid(),
            new DateTimeOffset(SystemClock.UtcNow, TimeSpan.Zero),
            SystemClock.UtcNow.Date,
            string.Empty,
            "application/json"));
        var cache = new FxRateCache(
            null, Microsoft.Extensions.Options.Options.Create(new FxSyncOptions { CacheTtlMinutes = 60 }),
            NullLogger<FxRateCache>.Instance);
        var provider = new PersistedFxRateProvider(cache, source);

        // Warm the cache so every subsequent call is served from it.
        for (var i = 0; i < 10; i++)
        {
            _ = await provider.GetRateAsync("USD", "EUR");
            _ = await provider.GetRateAsync("EUR", "JPY");
        }

        var watch = Stopwatch.StartNew();
        const int iterations = 500;
        for (var i = 0; i < iterations; i++)
        {
            _ = await provider.GetRateAsync("USD", "EUR");
            _ = await provider.GetRateAsync("EUR", "JPY");
            _ = await provider.GetRateAsync("USD", "JPY");
        }

        watch.Stop();
        var meanMs = watch.Elapsed.TotalMilliseconds / (iterations * 3);
        meanMs.Should().BeLessThan(50.0, $"cached conversion averaged {meanMs:0.###} ms/call (budget 50 ms)");
    }

    private static Dictionary<string, decimal> BuildPerfGrid()
    {
        var grid = new Dictionary<string, decimal> { ["EUR"] = 0.9m, ["JPY"] = 150m, ["GBP"] = 0.8m };
        for (var i = 0; i < 37; i++)
        {
            grid[$"C{i:D2}"] = 1m + (i * 0.01m);
        }

        return grid;
    }

    // ─────────────────── Helpers ───────────────────

    private RevenueReportService CreateService(ReportTestDbContext db)
        => new(_revenueEvents.Object, _runs.Object, db);

    private void SetupDailyTotals(params RevenueDailyTotal[] totals)
    {
        _revenueEvents
            .Setup(repository => repository.GetDailyTotalsAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(totals.ToList());
    }

    private static RevenueDailyTotal Total(DateTime day, string currency, decimal credit, decimal debit, int count)
        => new(day, currency, credit, debit, credit - debit, count);

    private async Task SeedBaseAsync(ReportTestDbContext db, string baseCurrency)
    {
        db.Set<CurrencyConfiguration>().Add(new CurrencyConfiguration
        {
            TenantId = _tenantId, CurrencyCode = baseCurrency, IsEnabled = true, IsBaseCurrency = true,
        });
        await db.SaveChangesAsync();
    }

    private static ReportTestDbContext NewDatabase()
    {
        var options = new DbContextOptionsBuilder<ReportTestDbContext>()
            .UseInMemoryDatabase($"fx-reports-{Guid.NewGuid():N}")
            .Options;
        return new ReportTestDbContext(options);
    }

    private static IActorContextAccessor SysAdminAccessor()
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(item => item.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string> { "SystemAdmin" },
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Test",
            IsAuthenticated = true,
        });
        return accessor.Object;
    }

    private static IActorContextAccessor TenantAccessor(Guid tenantId)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(item => item.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string> { "TenantAdmin" },
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Test",
            IsAuthenticated = true,
        });
        return accessor.Object;
    }

    private sealed class StubRateProvider : GameGuild.IExchangeRateProvider
    {
        public Task<ExchangeRate?> GetRateAsync(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken = default)
            => Task.FromResult<ExchangeRate?>(null); // current path unused in the historical test
    }

    /// <summary>Scope factory that never seeds: the perf/rate-source test needs no database.</summary>
    private sealed class EmptyScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => throw new NotSupportedException("not used in this test");
    }
}

/// <summary>In-memory context for the base-currency report tests.</summary>
internal sealed class ReportTestDbContext(DbContextOptions<ReportTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IDbContextTransaction>(NoOpTransaction.Instance);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CurrencyConfigurationConfiguration());
        modelBuilder.ApplyConfiguration(new FxRateSnapshotConfiguration());
        modelBuilder.ApplyConfiguration(new FxRateSyncRunConfiguration());
        base.OnModelCreating(modelBuilder);
    }

    private sealed class NoOpTransaction : IDbContextTransaction
    {
        public static readonly NoOpTransaction Instance = new();

        public Guid TransactionId { get => Guid.Empty; }

        public void Dispose() { }

        public ValueTask DisposeAsync() { return ValueTask.CompletedTask; }

        public void Commit() { }

        public void Rollback() { }

        public Task CommitAsync(CancellationToken cancellationToken = default) { return Task.CompletedTask; }

        public Task RollbackAsync(CancellationToken cancellationToken = default) { return Task.CompletedTask; }
    }
}
