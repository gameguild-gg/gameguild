using FluentAssertions;
using GameGuild;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using static GameGuild.Commerce.Payments.CurrencyConfigurationCommandHelpers;

namespace GameGuild.Commerce.Payments.UnitTests;

/// <summary>
///     Price localization (issue #412 spec C): manual per-market price overrides with
///     effective/expiry dating, FX-auto-calculation for every enabled currency, recalculation
///     runs with progress tracking and rollback, preview-before-apply, the admin override vs
///     auto-calculated view, override preservation during recalculation, and the post-sync
///     auto-price refresh hook.
/// </summary>
public class PriceLocalizationTests
{
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();
    private readonly ActorContext _actor;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _planId = Guid.NewGuid();

    public PriceLocalizationTests()
    {
        _actor = AuthenticatedActor();
        _actorAccessor.SetupGet(accessor => accessor.ActorContext).Returns(_actor);
    }

    // ────────────────────────────── Manual override command ──────────────────────────────

    [Fact]
    public async Task SetPlanLocalizedPrice_PersistsManualOverride_AndWritesAudit()
    {
        await using var db = NewDatabase();
        await SeedEnabledCurrenciesAsync(db, baseCurrency: "USD", "EUR");
        var handler = new SetPlanLocalizedPriceCommandHandler(
            db, new StubPlanResolver([_planId]), new NullLocalizedPlanPriceResolver(), _actorAccessor.Object);

        var id = await handler.Handle(
            new SetPlanLocalizedPriceCommand(_planId, "eur", 8.99m, "psychological pricing", null, null),
            CancellationToken.None);

        var row = db.Set<PriceLocalization>().Single();
        row.Id.Should().Be(id);
        row.CurrencyCode.Should().Be("EUR");
        row.LocalizedPrice.Should().Be(8.99m);
        row.IsAutoCalculated.Should().BeFalse();
        row.EffectiveDateUtc.Should().BeOnOrBefore(SystemClock.UtcNow);

        var audit = db.Set<AuditTrail>().Single();
        audit.Action.Should().Be(AuditAction.ConfigurationChanged);
        audit.Metadata.Should().Contain("psychological pricing");
    }

    [Fact]
    public async Task SetPlanLocalizedPrice_UnknownCurrency_FailsClosed()
    {
        await using var db = NewDatabase();
        var handler = new SetPlanLocalizedPriceCommandHandler(
            db, new StubPlanResolver([_planId]), new NullLocalizedPlanPriceResolver(), _actorAccessor.Object);

        var act = () => handler.Handle(
            new SetPlanLocalizedPriceCommand(_planId, "XXY", 8.99m, "reason"), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleViolationException>())
            .Which.Rule.Should().Be(CurrencyConfigurationRules.UnknownCurrency);
    }

    [Fact]
    public async Task SetPlanLocalizedPrice_DisabledCurrency_IsRejected()
    {
        await using var db = NewDatabase();
        await SeedEnabledCurrenciesAsync(db, baseCurrency: "USD");
        var handler = new SetPlanLocalizedPriceCommandHandler(
            db, new StubPlanResolver([_planId]), new NullLocalizedPlanPriceResolver(), _actorAccessor.Object);

        var act = () => handler.Handle(
            new SetPlanLocalizedPriceCommand(_planId, "EUR", 8.99m, "reason"), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleViolationException>())
            .Which.Rule.Should().Be(PriceLocalizationRules.CurrencyNotEnabled);
    }

    [Fact]
    public async Task SetPlanLocalizedPrice_MissingPlan_IsRejected()
    {
        await using var db = NewDatabase();
        await SeedEnabledCurrenciesAsync(db, baseCurrency: "USD", "EUR");
        var handler = new SetPlanLocalizedPriceCommandHandler(
            db, new StubPlanResolver([]), new NullLocalizedPlanPriceResolver(), _actorAccessor.Object);

        var act = () => handler.Handle(
            new SetPlanLocalizedPriceCommand(_planId, "EUR", 8.99m, "reason"), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleViolationException>())
            .Which.Rule.Should().Be(PriceLocalizationRules.PlanNotFound);
    }

    [Fact]
    public async Task SetPlanLocalizedPrice_FutureEffectiveDate_Schedules_AndExpiryIsValidated()
    {
        await using var db = NewDatabase();
        await SeedEnabledCurrenciesAsync(db, baseCurrency: "USD", "EUR");
        var handler = new SetPlanLocalizedPriceCommandHandler(
            db, new StubPlanResolver([_planId]), new NullLocalizedPlanPriceResolver(), _actorAccessor.Object);

        var future = DateTime.UtcNow.AddDays(7);
        await handler.Handle(
            new SetPlanLocalizedPriceCommand(_planId, "EUR", 9.49m, "scheduled increase",
                EffectiveDateUtc: future.AsUtcKind(), ExpiryDateUtc: future.AddDays(30).AsUtcKind()),
            CancellationToken.None);

        db.Set<PriceLocalization>().Single().EffectiveDateUtc.Should().Be(future.AsUtcKind());

        var act = () => handler.Handle(
            new SetPlanLocalizedPriceCommand(_planId, "EUR", 9.49m, "bad window",
                EffectiveDateUtc: future.AsUtcKind(), ExpiryDateUtc: future.AddDays(-1).AsUtcKind()),
            CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleViolationException>())
            .Which.Rule.Should().Be(PriceLocalizationRules.InvalidExpiry);
    }

    [Fact]
    public async Task ClearPlanLocalizedPrice_RemovesOnlyManualRows()
    {
        await using var db = NewDatabase();
        await SeedEnabledCurrenciesAsync(db, baseCurrency: "USD", "EUR");
        db.Set<PriceLocalization>().AddRange(
            new PriceLocalization
            {
                TenantId = _tenantId, PlanId = _planId, CurrencyCode = "EUR",
                LocalizedPrice = 8.99m, IsAutoCalculated = false, EffectiveDateUtc = SystemClock.UtcNow.AddDays(-1),
            },
            new PriceLocalization
            {
                TenantId = _tenantId, PlanId = _planId, CurrencyCode = "EUR",
                LocalizedPrice = 8.67m, IsAutoCalculated = true, EffectiveDateUtc = SystemClock.UtcNow.AddDays(-2),
            });
        await db.SaveChangesAsync();

        var handler = new ClearPlanLocalizedPriceCommandHandler(
            db, new NullLocalizedPlanPriceResolver(), _actorAccessor.Object);
        var removed = await handler.Handle(
            new ClearPlanLocalizedPriceCommand(_planId, "EUR", "revert to auto"), CancellationToken.None);

        removed.Should().Be(1);
        var remaining = db.Set<PriceLocalization>().Single();
        remaining.IsAutoCalculated.Should().BeTrue();
        db.Set<AuditTrail>().Single(audit => audit.Action == AuditAction.Deleted).Should().NotBeNull();
    }

    // ────────────────────────────── Recalculation: apply, preserve, progress, rollback ──────────────────────────────

    [Fact]
    public async Task Recalculate_CreatesAutoPricesForEnabledCurrencies_WithRateAndRun()
    {
        await using var db = NewDatabase();
        await SeedEnabledCurrenciesAsync(db, baseCurrency: "USD", "EUR", "JPY");
        var service = NewRecalculationService(db, rates: new Dictionary<(string From, string To), decimal>
        {
            [("USD", "EUR")] = 0.9m,
            [("USD", "JPY")] = 150m,
        });

        var outcome = await service.RecalculateAsync(db, _tenantId, planId: null, triggeredByUserId: Guid.NewGuid(), "manual");

        outcome.Status.Should().Be(PriceRecalculationRunStatus.Succeeded);
        outcome.TotalItems.Should().Be(2); // EUR + JPY (base USD excluded)
        outcome.CompletedItems.Should().Be(2);
        outcome.UpdatedPrices.Should().Be(2);
        outcome.PreservedOverrides.Should().Be(0);

        var rows = db.Set<PriceLocalization>().ToDictionary(row => row.CurrencyCode);
        rows["EUR"].LocalizedPrice.Should().Be(89.10m); // 99 USD × 0.9 → 89.1 (2 minor units)
        rows["EUR"].IsAutoCalculated.Should().BeTrue();
        rows["EUR"].ExchangeRateUsed.Should().Be(0.9m);
        rows["EUR"].LastCalculatedAt.Should().NotBeNull();
        rows["JPY"].LocalizedPrice.Should().Be(14850m); // 99 × 150, zero minor units

        var run = db.Set<PriceRecalculationRun>().Single();
        run.Status.Should().Be(PriceRecalculationRunStatus.Succeeded);
        run.TotalItems.Should().Be(2);
        run.CompletedItems.Should().Be(2);
        db.Set<AuditTrail>()
            .Should().Contain(audit => audit.EntityType == nameof(PriceRecalculationRun) && audit.Action == AuditAction.Created);
    }

    [Fact]
    public async Task Recalculate_PreservesManualOverrides_IncludingFutureScheduled()
    {
        await using var db = NewDatabase();
        await SeedEnabledCurrenciesAsync(db, baseCurrency: "USD", "EUR", "JPY");
        db.Set<PriceLocalization>().AddRange(
            new PriceLocalization
            {
                TenantId = _tenantId, PlanId = _planId, CurrencyCode = "EUR",
                LocalizedPrice = 8.99m, IsAutoCalculated = false, EffectiveDateUtc = SystemClock.UtcNow.AddDays(-1),
            },
            new PriceLocalization
            {
                TenantId = _tenantId, PlanId = _planId, CurrencyCode = "JPY",
                LocalizedPrice = 999m, IsAutoCalculated = false, EffectiveDateUtc = SystemClock.UtcNow.AddDays(5),
            });
        await db.SaveChangesAsync();

        var service = NewRecalculationService(db, rates: new Dictionary<(string From, string To), decimal>
        {
            [("USD", "EUR")] = 0.9m,
            [("USD", "JPY")] = 150m,
        });

        var outcome = await service.RecalculateAsync(db, _tenantId, planId: null, null, "manual");

        outcome.PreservedOverrides.Should().Be(2);
        outcome.UpdatedPrices.Should().Be(0);
        db.Set<PriceLocalization>().Single(row => row.CurrencyCode == "EUR").LocalizedPrice.Should().Be(8.99m);
        db.Set<PriceLocalization>().Single(row => row.CurrencyCode == "JPY").LocalizedPrice.Should().Be(999m);
    }

    [Fact]
    public async Task Recalculate_ExpiredManualOverride_LetsAutoPricingBackIn()
    {
        await using var db = NewDatabase();
        await SeedEnabledCurrenciesAsync(db, baseCurrency: "USD", "EUR");
        db.Set<PriceLocalization>().Add(new PriceLocalization
        {
            TenantId = _tenantId, PlanId = _planId, CurrencyCode = "EUR",
            LocalizedPrice = 7.77m, IsAutoCalculated = false,
            EffectiveDateUtc = SystemClock.UtcNow.AddDays(-10),
            ExpiryDateUtc = SystemClock.UtcNow.AddDays(-1), // fully expired
        });
        await db.SaveChangesAsync();

        var service = NewRecalculationService(db, rates: new Dictionary<(string From, string To), decimal>
        {
            [("USD", "EUR")] = 0.9m,
        });

        var outcome = await service.RecalculateAsync(db, _tenantId, planId: null, null, "manual");

        outcome.PreservedOverrides.Should().Be(0);
        outcome.UpdatedPrices.Should().Be(1);
        db.Set<PriceLocalization>().Single(row => !row.IsAutoCalculated && row.ExpiryDateUtc != null)
            .Should().NotBeNull(); // the expired manual row is untouched, not deleted
        var auto = db.Set<PriceLocalization>().Single(row => row.IsAutoCalculated);
        auto.LocalizedPrice.Should().Be(89.10m);
    }

    [Fact]
    public async Task Recalculate_MissingRate_FailsClosed_AndKeepsPriorValue()
    {
        await using var db = NewDatabase();
        await SeedEnabledCurrenciesAsync(db, baseCurrency: "USD", "EUR", "JPY");
        db.Set<PriceLocalization>().Add(new PriceLocalization
        {
            TenantId = _tenantId, PlanId = _planId, CurrencyCode = "JPY",
            LocalizedPrice = 14000m, IsAutoCalculated = true, EffectiveDateUtc = SystemClock.UtcNow.AddDays(-3),
        });
        await db.SaveChangesAsync();

        var service = NewRecalculationService(db, rates: new Dictionary<(string From, string To), decimal>
        {
            [("USD", "EUR")] = 0.9m, // JPY deliberately unavailable
        });

        var outcome = await service.RecalculateAsync(db, _tenantId, planId: null, null, "manual");

        outcome.SkippedForMissingRate.Should().Be(1);
        db.Set<PriceLocalization>().Single(row => row.CurrencyCode == "JPY").LocalizedPrice.Should().Be(14000m);
    }

    [Fact]
    public async Task Recalculate_SecondPass_UpdatesInPlace_AndRollbackRestoresBeforeValues()
    {
        await using var db = NewDatabase();
        await SeedEnabledCurrenciesAsync(db, baseCurrency: "USD", "EUR");
        var rates = new Dictionary<(string From, string To), decimal> { [("USD", "EUR")] = 0.9m };
        var service = NewRecalculationService(db, rates);
        var first = await service.RecalculateAsync(db, _tenantId, planId: null, null, "manual");
        db.Set<PriceLocalization>().Single().LocalizedPrice.Should().Be(89.10m);

        rates[("USD", "EUR")] = 0.95m;
        var second = await service.RecalculateAsync(db, _tenantId, planId: null, null, "manual");
        second.UpdatedPrices.Should().Be(1);
        db.Set<PriceLocalization>().Single().LocalizedPrice.Should().Be(94.05m);

        var rollbackHandler = new RollbackPriceRecalculationCommandHandler(
            db, new NullLocalizedPlanPriceResolver(), _actorAccessor.Object);
        var restored = await rollbackHandler.Handle(
            new RollbackPriceRecalculationCommand(second.RunId, "revert the rate-driven change"), CancellationToken.None);

        restored.Should().Be(1);
        db.Set<PriceLocalization>().Single().LocalizedPrice.Should().Be(89.10m);
        db.Set<PriceLocalization>().Single().ExchangeRateUsed.Should().Be(0.9m);
        db.Set<PriceRecalculationRun>().Single(run => run.Id == second.RunId)
            .Status.Should().Be(PriceRecalculationRunStatus.RolledBack);
        db.Set<AuditTrail>()
            .Should().Contain(audit => audit.Action == AuditAction.Restored && audit.Metadata!.Contains("RollbackPriceRecalculation"));
    }

    [Fact]
    public async Task Rollback_RunningRun_IsRejected()
    {
        await using var db = NewDatabase();
        db.Set<PriceRecalculationRun>().Add(new PriceRecalculationRun
        {
            TenantId = _tenantId, Status = PriceRecalculationRunStatus.Running, StartedAtUtc = SystemClock.UtcNow,
        });
        await db.SaveChangesAsync();

        var handler = new RollbackPriceRecalculationCommandHandler(
            db, new NullLocalizedPlanPriceResolver(), _actorAccessor.Object);

        var act = () => handler.Handle(
            new RollbackPriceRecalculationCommand(db.Set<PriceRecalculationRun>().Single().Id, "x"), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleViolationException>())
            .Which.Rule.Should().Be(PriceLocalizationRules.RunNotRollbackable);
    }

    // ────────────────────────────── Preview (no writes) ──────────────────────────────

    [Fact]
    public async Task Preview_ShowsProposedVsCurrent_WithoutWriting()
    {
        await using var db = NewDatabase();
        await SeedEnabledCurrenciesAsync(db, baseCurrency: "USD", "EUR");
        db.Set<PriceLocalization>().Add(new PriceLocalization
        {
            TenantId = _tenantId, PlanId = _planId, CurrencyCode = "EUR",
            LocalizedPrice = 8.99m, IsAutoCalculated = false, EffectiveDateUtc = SystemClock.UtcNow.AddDays(-1),
        });
        await db.SaveChangesAsync();

        var service = NewRecalculationService(db, rates: new Dictionary<(string From, string To), decimal>
        {
            [("USD", "EUR")] = 0.9m,
        });

        var preview = await service.PreviewAsync(db, _tenantId, planId: null);

        var item = preview.Single();
        item.IsManualOverride.Should().BeTrue();
        item.CurrentPrice.Should().Be(8.99m);
        item.ProposedPrice.Should().Be(89.10m);
        item.RateUsed.Should().Be(0.9m);
        item.Note.Should().Be("manual-override-preserved");

        db.Set<PriceLocalization>().Should().HaveCount(1); // nothing written
        db.Set<PriceRecalculationRun>().Should().BeEmpty();
    }

    // ────────────────────────────── Resolver: cycle ratio + expiry ──────────────────────────────

    [Fact]
    public async Task LocalizedPlanPriceResolver_AppliesEffectiveRow_AndScalesNonMonthlyByRatio()
    {
        await using var db = NewDatabase();
        await SeedEnabledCurrenciesAsync(db, baseCurrency: "USD", "EUR");
        db.Set<PriceLocalization>().Add(new PriceLocalization
        {
            TenantId = _tenantId, PlanId = _planId, CurrencyCode = "EUR",
            LocalizedPrice = 89.90m, IsAutoCalculated = true, EffectiveDateUtc = SystemClock.UtcNow.AddDays(-1),
        });
        await db.SaveChangesAsync();

        var resolver = new LocalizedPlanPriceResolver(
            db, new StubPlanResolver([_planId]), new Microsoft.Extensions.Caching.Memory.MemoryCache(
                new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

        var monthly = await resolver.ResolveLocalizedPriceAsync(
            _planId, BillingCycle.Monthly, new Money(99m, "USD"), "EUR");
        monthly!.Amount.Should().Be(89.90m); // monthly = the localized value itself

        var annual = await resolver.ResolveLocalizedPriceAsync(
            _planId, BillingCycle.Annually, new Money(990m, "USD"), "EUR");
        annual!.Amount.Should().Be(899.00m); // 89.90 × (990/99): interval discount preserved

        // Expired rows stop applying.
        db.Set<PriceLocalization>().Single().ExpiryDateUtc = SystemClock.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        resolver.Invalidate(_planId, "EUR");
        var expired = await resolver.ResolveLocalizedPriceAsync(
            _planId, BillingCycle.Monthly, new Money(99m, "USD"), "EUR");
        expired.Should().BeNull();
    }

    // ────────────────────────────── FX sync hook: auto prices follow rate updates ──────────────────────────────

    [Fact]
    public async Task FxSync_WithChangedRates_RefreshesAutoLocalizedPrices()
    {
        var (db, serviceProvider) = NewDbAndProvider();
        var tenantId = Guid.NewGuid();
        db.Set<CurrencyConfiguration>().AddRange(
            EnabledCurrency(tenantId, "USD", isBase: true),
            EnabledCurrency(tenantId, "EUR"));
        db.Set<PriceLocalization>().Add(new PriceLocalization
        {
            TenantId = tenantId, PlanId = _planId, CurrencyCode = "EUR",
            LocalizedPrice = 50m, IsAutoCalculated = true, EffectiveDateUtc = SystemClock.UtcNow.AddDays(-1),
        });
        await db.SaveChangesAsync();

        var rates = new Dictionary<(string From, string To), decimal> { [("USD", "EUR")] = 0.9m };
        var planResolver = new StubPlanResolver([_planId]);
        var rateProvider = new StubRateProvider(rates);
        var recalculation = new PriceLocalizationRecalculationService(
            planResolver, rateProvider, new NullLocalizedPlanPriceResolver());

        var runtime = new FxRuntimeRateSource(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(), NullLogger<FxRuntimeRateSource>.Instance);
        var alerter = new Mock<IFxRateSyncAlerter>();
        var options = new FxSyncOptions
        {
            Provider = FxSyncOptions.ProviderOpenExchangeRates,
            RetryMaxAttempts = 1,
            RetryBaseDelaySeconds = 0,
            AlertAfterConsecutiveFailures = 1,
            StalenessAlertDays = 2,
        };

        var services = new ServiceCollection();
        services.AddSingleton<IApplicationDbContext>(db);
        services.AddSingleton(planResolver);
        services.AddSingleton<GameGuild.IExchangeRateProvider>(rateProvider);
        services.AddSingleton<ILocalizedPlanPriceResolver>(new NullLocalizedPlanPriceResolver());
        services.AddSingleton<IPriceLocalizationRecalculationService>(recalculation);
        using var richProvider = services.BuildServiceProvider();

        var sync = new FxRateSyncService(
            new IFxRateProvider[] { new StubSyncProvider(_ => new FxRateSet(
                "openexchangerates", "USD",
                new Dictionary<string, decimal> { ["EUR"] = 0.9m },
                new DateTimeOffset(SystemClock.UtcNow, TimeSpan.Zero),
                SystemClock.UtcNow.Date,
                "raw", "application/json")) },
            richProvider, Options.Create(options), runtime, alerter.Object,
            NullLogger<FxRateSyncService>.Instance);

        var result = await sync.SyncAsync();

        result.Status.Should().Be(FxRateSyncRunStatus.Succeeded);
        db.Set<PriceLocalization>()
            .Single(row => row.TenantId == tenantId && row.CurrencyCode == "EUR")
            .LocalizedPrice.Should().Be(89.10m); // refreshed from 50 to 99 × 0.9
        db.Set<PriceRecalculationRun>()
            .Single(run => run.TenantId == tenantId).TriggerReason.Should().Be("fx-sync");
    }

    // ────────────────────────────── Helpers ──────────────────────────────

    private PriceLocalizationRecalculationService NewRecalculationService(
        PriceLocalizationTestDbContext db,
        Dictionary<(string From, string To), decimal> rates)
    {
        return new PriceLocalizationRecalculationService(
            new StubPlanResolver([_planId]),
            new StubRateProvider(rates),
            new NullLocalizedPlanPriceResolver());
    }

    private async Task SeedEnabledCurrenciesAsync(PriceLocalizationTestDbContext db, string baseCurrency, params string[] enabled)
    {
        db.Set<CurrencyConfiguration>().Add(EnabledCurrency(_tenantId, baseCurrency, isBase: true));
        foreach (var code in enabled.Where(code => code != baseCurrency))
        {
            db.Set<CurrencyConfiguration>().Add(EnabledCurrency(_tenantId, code));
        }

        await db.SaveChangesAsync();
    }

    private static CurrencyConfiguration EnabledCurrency(Guid tenantId, string code, bool isBase = false) => new()
    {
        TenantId = tenantId,
        CurrencyCode = code,
        IsEnabled = true,
        IsBaseCurrency = isBase,
    };

    private PriceLocalizationTestDbContext NewDatabase()
    {
        var options = new DbContextOptionsBuilder<PriceLocalizationTestDbContext>()
            .UseInMemoryDatabase($"price-localization-{Guid.NewGuid():N}")
            .Options;
        return new PriceLocalizationTestDbContext(options);
    }

    private (PriceLocalizationTestDbContext Db, ServiceProvider Provider) NewDbAndProvider()
    {
        var db = NewDatabase();
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationDbContext>(db);
        return (db, services.BuildServiceProvider());
    }

    private ActorContext AuthenticatedActor() => new()
    {
        ActorKind = ActorKind.User,
        SubjectId = Guid.NewGuid().ToString(),
        TenantId = _tenantId,
        Roles = new HashSet<string> { "SystemAdmin" },
        Permissions = new HashSet<string>(),
        TypedAttributes = ActorAttributes.Empty,
        AuthScheme = "Test",
        IsAuthenticated = true,
    };

    private sealed class StubPlanResolver(Guid[] planIds) : IPlanPricingResolver
    {
        public Task<Money?> GetPlanMonthlyPriceAsync(Guid planId, CancellationToken cancellationToken = default)
            => Task.FromResult<Money?>(new Money(99m, "USD"));

        public Task<Money?> GetPlanPriceAsync(Guid planId, BillingCycle billingCycle, CancellationToken cancellationToken = default)
            => Task.FromResult<Money?>(billingCycle == BillingCycle.Annually ? new Money(990m, "USD") : new Money(99m, "USD"));

        public Task<bool> PlanExistsAsync(Guid planId, CancellationToken cancellationToken = default)
            => Task.FromResult(planIds.Contains(planId));

        public Task<IReadOnlyList<Guid>> GetActivePlanIdsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Guid>>(planIds);
    }

    private sealed class StubRateProvider(Dictionary<(string From, string To), decimal> rates) : GameGuild.IExchangeRateProvider
    {
        public Task<ExchangeRate?> GetRateAsync(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken = default)
        {
            if (rates.TryGetValue((baseCurrency.ToUpperInvariant(), quoteCurrency.ToUpperInvariant()), out var rate))
            {
                return Task.FromResult<ExchangeRate?>(new ExchangeRate(
                    baseCurrency.ToUpperInvariant(), quoteCurrency.ToUpperInvariant(), rate,
                    new DateTimeOffset(SystemClock.UtcNow, TimeSpan.Zero), "stub"));
            }

            return Task.FromResult<ExchangeRate?>(null);
        }
    }

    private sealed class StubSyncProvider(Func<int, FxRateSet> behavior) : IFxRateProvider
    {
        public string Name { get; } = "openexchangerates";

        public Task<FxRateSet> GetRatesAsync(DateTimeOffset? asOfUtc = null, CancellationToken cancellationToken = default)
            => Task.FromResult(behavior(0));
    }
}

/// <summary>
///     In-memory context for the price localization tests. CurrencyConfiguration rows seeded
///     per test; the no-op transaction mirrors the currency configuration test context.
/// </summary>
internal sealed class PriceLocalizationTestDbContext(DbContextOptions<PriceLocalizationTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IDbContextTransaction>(NoOpTransaction.Instance);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CurrencyConfigurationConfiguration());
        modelBuilder.ApplyConfiguration(new FxRateSnapshotConfiguration());
        modelBuilder.ApplyConfiguration(new FxRateSyncRunConfiguration());
        modelBuilder.ApplyConfiguration(new PriceLocalizationConfiguration());
        modelBuilder.ApplyConfiguration(new PriceRecalculationRunConfiguration());
        modelBuilder.ApplyConfiguration(new AuditTrailConfiguration());
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

internal static class DateTimeTestExtensions
{
    public static DateTime AsUtcKind(this DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
