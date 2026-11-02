using System.Net;
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

namespace GameGuild.Commerce.Payments.UnitTests;

/// <summary>
///     Security and compliance layer of the currency/FX foundation (issue #412 spec H):
///     encrypted-at-rest FX provider API keys, rate limiting of outbound provider calls,
///     rate-reasonableness anomaly detection (&gt; 10% daily change quarantine) and the
///     GDPR-minimized location-based currency suggestion.
/// </summary>
public class FxSecurityTests
{
    private readonly Mock<IFxRateSyncAlerter> _alerter = new();
    private readonly Mock<IGatewayCredentialStore> _credentialStore = new();
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();

    public FxSecurityTests()
    {
        _actorAccessor.SetupGet(accessor => accessor.ActorContext).Returns(new ActorContext
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
    }

    // ────────────────────────────── Anomaly detection (>10% daily change) ──────────────────────────────

    [Fact]
    public async Task Sync_RateMovedBeyondThreshold_IsQuarantined_AuditedAndAlerted()
    {
        var (service, db, _) = BuildSyncService(
            new StubProvider(_ => RateSet(new Dictionary<string, decimal> { ["EUR"] = 1.2m })));
        var yesterday = SystemClock.UtcNow.Date.AddDays(-1);
        db.Set<FxRateSnapshot>().Add(new FxRateSnapshot
        {
            BaseCurrency = "USD", QuoteCurrency = "EUR", Rate = 0.9m, // +33% → anomaly
            EffectiveDateUtc = yesterday, AsOfUtc = yesterday.AddHours(12),
            ProviderName = "openexchangerates", Source = "latest", IsOverride = false,
        });
        await db.SaveChangesAsync();

        var result = await service.SyncAsync();

        result.RatesApplied.Should().Be(0);
        var run = db.Set<FxRateSyncRun>().Single();
        run.AnomalousRatesSkipped.Should().Be(1);
        run.AnomalyDetails.Should().Contain("USD/EUR").And.Contain("0.9").And.Contain("1.2");

        // Fail closed: the prior quotation stays authoritative, nothing new persisted for EUR.
        db.Set<FxRateSnapshot>().Single(snapshot => snapshot.QuoteCurrency == "EUR").Rate.Should().Be(0.9m);

        db.Set<AuditTrail>()
            .Should().Contain(audit => audit.Metadata!.Contains("FxRateAnomalySkipped"));
        _alerter.Verify(
            alerter => alerter.RaiseRateAnomalyAlertAsync(
                It.Is<IReadOnlyList<(string Pair, decimal OldRate, decimal NewRate, decimal ChangePercent)>>(
                    anomalies => anomalies.Count == 1 && anomalies[0].Pair == "USD/EUR"),
                10m,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Sync_RateMovedWithinThreshold_IsAppliedWithoutAnomaly()
    {
        var (service, db, _) = BuildSyncService(
            new StubProvider(_ => RateSet(new Dictionary<string, decimal> { ["EUR"] = 0.91m })));
        var yesterday = SystemClock.UtcNow.Date.AddDays(-1);
        db.Set<FxRateSnapshot>().Add(new FxRateSnapshot
        {
            BaseCurrency = "USD", QuoteCurrency = "EUR", Rate = 0.9m, // +1.1% → fine
            EffectiveDateUtc = yesterday, AsOfUtc = yesterday.AddHours(12),
            ProviderName = "openexchangerates", Source = "latest", IsOverride = false,
        });
        await db.SaveChangesAsync();

        var result = await service.SyncAsync();

        result.RatesApplied.Should().Be(1);
        db.Set<FxRateSyncRun>().Single().AnomalousRatesSkipped.Should().Be(0);
        _alerter.Verify(
            alerter => alerter.RaiseRateAnomalyAlertAsync(
                It.IsAny<IReadOnlyList<(string, decimal, decimal, decimal)>>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Sync_FirstEverGrid_HasNoBaseline_AndAppliesEverything()
    {
        var (service, db, _) = BuildSyncService(
            new StubProvider(_ => RateSet(new Dictionary<string, decimal> { ["EUR"] = 0.9m, ["JPY"] = 150m })));

        var result = await service.SyncAsync();

        result.RatesApplied.Should().Be(2);
        db.Set<FxRateSnapshot>().Should().HaveCount(2);
        db.Set<FxRateSyncRun>().Single().AnomalousRatesSkipped.Should().Be(0);
    }

    [Fact]
    public void FxSyncOptions_AnomalyAndRateLimitBounds_AreValidated()
    {
        var validator = new FxSyncOptionsValidator();

        validator.Validate(null, new FxSyncOptions { MaxRateChangePercentPerDay = -1m }).Failed.Should().BeTrue();
        validator.Validate(null, new FxSyncOptions { ProviderMaxCallsPerMinute = -1 }).Failed.Should().BeTrue();
        validator.Validate(null, new FxSyncOptions()).Succeeded.Should().BeTrue(); // defaults 10% / 30 per minute
    }

    // ────────────────────────────── Provider call rate limiting ──────────────────────────────

    [Fact]
    public void ProviderRateLimiter_CapsCallsPerWindow_AndSlides()
    {
        var limiter = new FxProviderRateLimiterHandler(
            Options.Create(new FxSyncOptions { ProviderMaxCallsPerMinute = 3 }));
        var window = TimeSpan.FromMinutes(1);
        var start = DateTime.UtcNow;

        limiter.TryAcquire(start, 3, window).Should().BeTrue();
        limiter.TryAcquire(start.AddSeconds(1), 3, window).Should().BeTrue();
        limiter.TryAcquire(start.AddSeconds(2), 3, window).Should().BeTrue();
        limiter.TryAcquire(start.AddSeconds(3), 3, window).Should().BeFalse(); // window exhausted

        // The oldest call leaves the window; capacity returns.
        limiter.TryAcquire(start.AddSeconds(61), 3, window).Should().BeTrue();
    }

    [Fact]
    public void ProviderRateLimiter_ZeroLimit_DisablesTheGuard()
    {
        var limiter = new FxProviderRateLimiterHandler(
            Options.Create(new FxSyncOptions { ProviderMaxCallsPerMinute = 0 }));
        var now = DateTime.UtcNow;

        for (var i = 0; i < 100; i++)
        {
            limiter.TryAcquire(now, 0, TimeSpan.FromMinutes(1)).Should().BeTrue();
        }
    }

    // ────────────────────────────── Encrypted FX provider API keys ──────────────────────────────

    [Fact]
    public async Task ProvisionFxProviderApiKey_StoresEncryptedSlot_AndAuditsFingerprintOnly()
    {
        await using var db = NewDatabase();
        GatewayCredentialProvisionResult? provisioned = null;
        _credentialStore
            .Setup(store => store.ProvisionAsync(
                "fx:openexchangerates", "app-id", "secret-app-id", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => provisioned = new GatewayCredentialProvisionResult(
                "fx:openexchangerates", "app-id", "abc123fingerprint", SystemClock.UtcNow, DeactivatedPrevious: false))
            .Verifiable();

        var handler = new ProvisionFxProviderApiKeyCommandHandler(
            _credentialStore.Object, db, _actorAccessor.Object);
        var result = await handler.Handle(
            new ProvisionFxProviderApiKeyCommand("OpenExchangeRates", "secret-app-id"), CancellationToken.None);

        result.Fingerprint.Should().Be("abc123fingerprint");
        _credentialStore.VerifyAll();

        var audit = db.Set<AuditTrail>().Single();
        audit.NewValue.Should().Contain("abc123fingerprint");
        audit.NewValue.Should().NotContain("secret-app-id"); // the key itself never lands anywhere
    }

    [Fact]
    public async Task ProvisionFxProviderApiKey_NonKeyBearingProvider_FailsClosed()
    {
        await using var db = NewDatabase();
        var handler = new ProvisionFxProviderApiKeyCommandHandler(
            _credentialStore.Object, db, _actorAccessor.Object);

        var act = () => handler.Handle(
            new ProvisionFxProviderApiKeyCommand("ecb", "no-key-needed"), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleViolationException>())
            .Which.Rule.Should().Be("FxProviderRequiresNoApiKey");
        _credentialStore.Verify(
            store => store.ProvisionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OpenExchangeRatesProvider_PrefersTheEncryptedCredentialOverConfiguration()
    {
        var handler = new RecordingHandler("""{"disclaimer":"x","license":"y","timestamp":1730000000,"base":"USD","date":"2026-10-28","rates":{"EUR":0.9}}""", HttpStatusCode.OK);
        var store = new Mock<IGatewayCredentialStore>();
        store
            .Setup(item => item.GetActiveAsync("fx:openexchangerates", "app-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayCredentialSecret("fx:openexchangerates", "app-id", "stored-app-id", "fp", SystemClock.UtcNow));

        var provider = new OpenExchangeRatesFxRateProvider(
            new HttpClient(handler),
            Options.Create(new FxSyncOptions
            {
                Provider = FxSyncOptions.ProviderOpenExchangeRates,
                OpenExchangeRates = new FxSyncOptions.OpenExchangeRatesOptions { AppId = "config-app-id" },
            }),
            NullLogger<OpenExchangeRatesFxRateProvider>.Instance,
            store.Object);

        await provider.GetRatesAsync();

        handler.LastUrl.Should().Contain("app_id=stored-app-id");
        handler.LastUrl.Should().NotContain("config-app-id");
    }

    [Fact]
    public async Task OpenExchangeRatesProvider_WithoutAnyKey_NeverDialsOut()
    {
        var handler = new RecordingHandler("{}", HttpStatusCode.OK);
        var provider = new OpenExchangeRatesFxRateProvider(
            new HttpClient(handler),
            Options.Create(new FxSyncOptions
            {
                Provider = FxSyncOptions.ProviderOpenExchangeRates,
                OpenExchangeRates = new FxSyncOptions.OpenExchangeRatesOptions { AppId = "" },
            }),
            NullLogger<OpenExchangeRatesFxRateProvider>.Instance,
            _credentialStore.Object); // store has no active credential

        var act = () => provider.GetRatesAsync();

        await act.Should().ThrowAsync<FxRateProviderUnavailableException>();
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task DeactivateFxProviderApiKey_DelegatesToTheStore()
    {
        _credentialStore
            .Setup(store => store.DeactivateAsync("fx:openexchangerates", "app-id", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new DeactivateFxProviderApiKeyCommandHandler(_credentialStore.Object, _actorAccessor.Object);
        var deactivated = await handler.Handle(
            new DeactivateFxProviderApiKeyCommand("openexchangerates"), CancellationToken.None);

        deactivated.Should().BeTrue();
    }

    // ────────────────────────────── GDPR-minimized currency suggestion ──────────────────────────────

    [Theory]
    [InlineData("DE", "EUR")]
    [InlineData("US", "USD")]
    [InlineData("JP", "JPY")]
    [InlineData("BR", "BRL")]
    public void CountryToCurrency_MapsThroughTheIsoCatalog(string country, string expected)
    {
        GetCurrencySuggestionQueryHandler.MapCountryToCurrency(country).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("D")]
    [InlineData("DEU")]
    [InlineData("1A")]
    public void CountryToCurrency_UnknownOrMalformed_CountryReturnsNull(string country)
    {
        GetCurrencySuggestionQueryHandler.MapCountryToCurrency(country).Should().BeNull();
    }

    [Fact]
    public async Task Suggestion_TenantEnabledCurrency_Wins()
    {
        await using var db = NewDatabase();
        var tenantId = Guid.NewGuid();
        db.Set<CurrencyConfiguration>().AddRange(
            new CurrencyConfiguration { TenantId = tenantId, CurrencyCode = "USD", IsEnabled = true, IsBaseCurrency = true },
            new CurrencyConfiguration { TenantId = tenantId, CurrencyCode = "EUR", IsEnabled = true });
        await db.SaveChangesAsync();
        var handler = new GetCurrencySuggestionQueryHandler(db, ActorAccessor(tenantId));

        var suggestion = await handler.Handle(new GetCurrencySuggestionQuery("DE"), CancellationToken.None);

        suggestion.SuggestedCurrency.Should().Be("EUR");
        suggestion.Source.Should().Be("edge-header");
        suggestion.IsEnabledForTenant.Should().BeTrue();
        suggestion.PrivacyNotice.Should().Contain("No IP address");
    }

    [Fact]
    public async Task Suggestion_CountryCurrencyDisabled_FallsBackToTenantBase()
    {
        await using var db = NewDatabase();
        var tenantId = Guid.NewGuid();
        db.Set<CurrencyConfiguration>().Add(new CurrencyConfiguration
        {
            TenantId = tenantId, CurrencyCode = "USD", IsEnabled = true, IsBaseCurrency = true,
        });
        await db.SaveChangesAsync();
        var handler = new GetCurrencySuggestionQueryHandler(db, ActorAccessor(tenantId));

        var suggestion = await handler.Handle(new GetCurrencySuggestionQuery("DE"), CancellationToken.None);

        suggestion.SuggestedCurrency.Should().Be("USD"); // EUR is not enabled for this tenant
        suggestion.Source.Should().Be("tenant-base");
        suggestion.FallbackReason.Should().Be("currency-not-enabled");
    }

    [Fact]
    public async Task Suggestion_AnonymousCaller_GetsCatalogMapping_WithoutTenantClaims()
    {
        await using var db = NewDatabase();
        var anonymous = new Mock<IActorContextAccessor>();
        anonymous.SetupGet(accessor => accessor.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = null,
            TenantId = null,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Test",
            IsAuthenticated = false,
        });
        var handler = new GetCurrencySuggestionQueryHandler(db, anonymous.Object);

        var suggestion = await handler.Handle(new GetCurrencySuggestionQuery("JP"), CancellationToken.None);

        suggestion.SuggestedCurrency.Should().Be("JPY");
        suggestion.IsEnabledForTenant.Should().BeFalse();
        suggestion.Source.Should().Be("edge-header");
    }

    // ────────────────────────────── Helpers ──────────────────────────────

    private static FxRateSet RateSet(IReadOnlyDictionary<string, decimal> rates)
    {
        var asOf = SystemClock.UtcNow;
        return new FxRateSet(
            "openexchangerates", "USD",
            new Dictionary<string, decimal>(rates),
            new DateTimeOffset(asOf, TimeSpan.Zero),
            asOf.Date,
            "raw-openexchangerates",
            "application/json");
    }

    private (FxRateSyncService Service, FxSecurityTestDbContext Db, FxRuntimeRateSource Runtime) BuildSyncService(
        StubProvider primary)
    {
        var (db, provider) = NewDbAndProvider();
        var runtime = new FxRuntimeRateSource(
            provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<FxRuntimeRateSource>.Instance);
        var options = new FxSyncOptions
        {
            Provider = FxSyncOptions.ProviderOpenExchangeRates,
            RetryMaxAttempts = 1,
            RetryBaseDelaySeconds = 0,
            AlertAfterConsecutiveFailures = 1,
            StalenessAlertDays = 2,
            MaxRateChangePercentPerDay = 10m,
        };
        var service = new FxRateSyncService(
            new IFxRateProvider[] { primary }, provider, Options.Create(options), runtime, _alerter.Object,
            NullLogger<FxRateSyncService>.Instance);
        return (service, db, runtime);
    }

    private static (FxSecurityTestDbContext Db, ServiceProvider Provider) NewDbAndProvider()
    {
        var options = new DbContextOptionsBuilder<FxSecurityTestDbContext>()
            .UseInMemoryDatabase($"fx-security-{Guid.NewGuid():N}")
            .Options;
        var db = new FxSecurityTestDbContext(options);
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationDbContext>(db);
        return (db, services.BuildServiceProvider());
    }

    private FxSecurityTestDbContext NewDatabase()
    {
        var options = new DbContextOptionsBuilder<FxSecurityTestDbContext>()
            .UseInMemoryDatabase($"fx-security-{Guid.NewGuid():N}")
            .Options;
        return new FxSecurityTestDbContext(options);
    }

    private static IActorContextAccessor ActorAccessor(Guid tenantId)
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

    private sealed class StubProvider(Func<int, FxRateSet> behavior) : IFxRateProvider
    {
        public string Name { get; } = "openexchangerates";

        public Task<FxRateSet> GetRatesAsync(DateTimeOffset? asOfUtc = null, CancellationToken cancellationToken = default)
            => Task.FromResult(behavior(0));
    }

    private sealed class RecordingHandler(string payload, HttpStatusCode status) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public string? LastUrl { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastUrl = request.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(payload),
            });
        }
    }
}

/// <summary>In-memory context for the FX security tests.</summary>
internal sealed class FxSecurityTestDbContext(DbContextOptions<FxSecurityTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IDbContextTransaction>(NoOpTransaction.Instance);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CurrencyConfigurationConfiguration());
        modelBuilder.ApplyConfiguration(new FxRateSnapshotConfiguration());
        modelBuilder.ApplyConfiguration(new FxRateSyncRunConfiguration());
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
