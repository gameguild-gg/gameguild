using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.CurrencyConversion;

/// <summary>
///     Currency conversion query (issue #412 spec D): current-rate conversion through the FX
///     seam, historical as-of lookups against persisted snapshots (direct and inverted
///     direction), identity entries, enabled-currency gating for both legs, batch resilience
///     and minor-unit rounding to the target currency's ISO 4217 exponent.
/// </summary>
public class CurrencyConversionCommandTests
{
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();
    private readonly ActorContext _actor;

    public CurrencyConversionCommandTests()
    {
        _actor = AuthenticatedActor();
        _actorAccessor.SetupGet(accessor => accessor.ActorContext).Returns(_actor);
    }

    [Fact]
    public async Task Convert_CurrentRate_ConvertsAndReportsQuotation()
    {
        await using var db = NewDatabase();
        await EnableAsync(db, "USD", "EUR");
        var rates = new ManualExchangeRateProvider(
        [
            new ExchangeRate("USD", "EUR", 0.9m, DateTimeOffset.UtcNow, "manual")
        ]);

        var result = await HandleAsync(db, rates, new ConvertCurrencyCommand(
            "EUR",
            [new CurrencyConversionItemInput(100m, "USD")]));

        var item = result.Items.Should().ContainSingle().Subject;
        item.Error.Should().BeNull();
        item.ConvertedAmount.Should().Be(90.00m);
        item.Rate.Should().Be(0.9m);
        item.RateSource.Should().Be("manual");
        item.IsIdentity.Should().BeFalse();
        result.TargetCurrency.Should().Be("EUR");
    }

    [Fact]
    public async Task Convert_ZeroDecimalTarget_RoundsToMinorUnitExponent()
    {
        await using var db = NewDatabase();
        await EnableAsync(db, "USD", "JPY");
        var rates = new ManualExchangeRateProvider(
        [
            new ExchangeRate("USD", "JPY", 149.555m, DateTimeOffset.UtcNow, "manual")
        ]);

        var result = await HandleAsync(db, rates, new ConvertCurrencyCommand(
            "JPY",
            [new CurrencyConversionItemInput(10m, "USD")]));

        // JPY has 0 minor-unit digits: 1495.55 must round to the integer 1496 (AwayFromZero).
        result.Items.Single().ConvertedAmount.Should().Be(1496m);
    }

    [Fact]
    public async Task Convert_SameCurrency_IsIdentityWithoutQuotation()
    {
        await using var db = NewDatabase();
        await EnableAsync(db, "EUR");

        var result = await HandleAsync(db, new ManualExchangeRateProvider([]), new ConvertCurrencyCommand(
            "EUR",
            [new CurrencyConversionItemInput(42.5m, "eur")]));

        var item = result.Items.Single();
        item.IsIdentity.Should().BeTrue();
        item.ConvertedAmount.Should().Be(42.5m);
        item.Rate.Should().BeNull();
        item.Error.Should().BeNull();
    }

    [Fact]
    public async Task Convert_MissingRate_FailsThatItemButNotTheBatch()
    {
        await using var db = NewDatabase();
        await EnableAsync(db, "USD", "EUR", "GBP");
        var rates = new ManualExchangeRateProvider(
        [
            new ExchangeRate("USD", "EUR", 0.9m, DateTimeOffset.UtcNow, "manual")
        ]);

        var result = await HandleAsync(db, rates, new ConvertCurrencyCommand(
            "EUR",
            [
                new CurrencyConversionItemInput(100m, "GBP"), // no GBP quotation
                new CurrencyConversionItemInput(100m, "USD"), // served
            ]));

        result.Items.Should().HaveCount(2);
        result.Items[0].Error.Should().Contain("No exchange rate available");
        result.Items[0].ConvertedAmount.Should().BeNull();
        result.Items[1].Error.Should().BeNull();
        result.Items[1].ConvertedAmount.Should().Be(90.00m);
    }

    [Fact]
    public async Task Convert_DisabledSourceCurrency_FailsThatItem()
    {
        await using var db = NewDatabase();
        await EnableAsync(db, "USD", "EUR"); // GBP never enabled
        var rates = new ManualExchangeRateProvider(
        [
            new ExchangeRate("GBP", "EUR", 1.15m, DateTimeOffset.UtcNow, "manual")
        ]);

        var result = await HandleAsync(db, rates, new ConvertCurrencyCommand(
            "EUR",
            [new CurrencyConversionItemInput(100m, "GBP")]));

        result.Items.Single().Error.Should().Contain("not enabled for this tenant");
    }

    [Fact]
    public async Task Convert_DisabledTargetCurrency_FailsClosedForWholeRequest()
    {
        await using var db = NewDatabase();
        await EnableAsync(db, "USD"); // target EUR not enabled

        var act = () => HandleAsync(db, new ManualExchangeRateProvider([]), new ConvertCurrencyCommand(
            "EUR",
            [new CurrencyConversionItemInput(100m, "USD")]));

        (await act.Should().ThrowAsync<BusinessRuleViolationException>())
            .Which.Rule.Should().Be(CurrencyConversionRules.TargetCurrencyNotEnabled);
    }

    [Fact]
    public async Task Convert_UnknownTargetCode_FailsClosed()
    {
        await using var db = NewDatabase();

        var act = () => HandleAsync(db, new ManualExchangeRateProvider([]), new ConvertCurrencyCommand(
            "XXY",
            [new CurrencyConversionItemInput(100m, "USD")]));

        (await act.Should().ThrowAsync<BusinessRuleViolationException>())
            .Which.Rule.Should().Be(CurrencyConfigurationRules.UnknownCurrency);
    }

    [Fact]
    public async Task Convert_HistoricalAsOf_UsesNewestEffectiveSnapshotAtOrBeforeDate()
    {
        await using var db = NewDatabase();
        await EnableAsync(db, "USD", "EUR");
        await AddSnapshotAsync(db, "USD", "EUR", 0.8m, effectiveDate: new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc));
        await AddSnapshotAsync(db, "USD", "EUR", 0.9m, effectiveDate: new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc));
        await AddSnapshotAsync(db, "USD", "EUR", 0.95m, effectiveDate: new DateTime(2026, 03, 01, 0, 0, 0, DateTimeKind.Utc)); // after as-of: excluded

        var result = await HandleAsync(db, new ManualExchangeRateProvider([]), new ConvertCurrencyCommand(
            "EUR",
            [new CurrencyConversionItemInput(100m, "USD")],
            AsOfUtc: new DateTime(2026, 02, 15, 12, 0, 0, DateTimeKind.Utc)));

        var item = result.Items.Single();
        item.Error.Should().BeNull();
        item.Rate.Should().Be(0.9m);
        item.ConvertedAmount.Should().Be(90.00m);
        item.RateEffectiveDateUtc.Should().Be(new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc));
        item.RateProvider.Should().NotBeNull();
    }

    [Fact]
    public async Task Convert_HistoricalAsOf_InvertsOppositePairWhenDirectIsMissing()
    {
        await using var db = NewDatabase();
        await EnableAsync(db, "USD", "EUR");
        // Only EUR→USD persisted; the requested USD→EUR must use its documented inverse.
        await AddSnapshotAsync(db, "EUR", "USD", 1.25m, effectiveDate: new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc));

        var result = await HandleAsync(db, new ManualExchangeRateProvider([]), new ConvertCurrencyCommand(
            "EUR",
            [new CurrencyConversionItemInput(100m, "USD")],
            AsOfUtc: new DateTime(2026, 01, 15, 0, 0, 0, DateTimeKind.Utc)));

        var item = result.Items.Single();
        item.Error.Should().BeNull();
        item.Rate.Should().BeApproximately(0.8m, 0.000000000001m);
        item.ConvertedAmount.Should().Be(80.00m);
        item.RateSource.Should().Contain("inverted");
    }

    [Fact]
    public async Task Convert_HistoricalAsOf_NoSnapshot_FailsThatItem()
    {
        await using var db = NewDatabase();
        await EnableAsync(db, "USD", "EUR");

        var result = await HandleAsync(db, new ManualExchangeRateProvider([]), new ConvertCurrencyCommand(
            "EUR",
            [new CurrencyConversionItemInput(100m, "USD")],
            AsOfUtc: new DateTime(2020, 01, 01, 0, 0, 0, DateTimeKind.Utc)));

        result.Items.Single().Error.Should().Contain("No historical exchange rate available");
    }

    [Fact]
    public async Task Convert_UnauthenticatedActor_IsRejected()
    {
        await using var db = NewDatabase();
        var anonymous = new Mock<IActorContextAccessor>();
        anonymous.SetupGet(accessor => accessor.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Test",
            IsAuthenticated = false
        });

        var handler = new ConvertCurrencyCommandHandler(db, new ManualExchangeRateProvider([]), anonymous.Object);
        var act = () => handler.Handle(new ConvertCurrencyCommand("EUR", [new CurrencyConversionItemInput(1m, "USD")]), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Convert_Validator_RejectsEmptyAndOversizedBatchesAndFutureDates()
    {
        var validator = new ConvertCurrencyCommandValidator();

        var empty = await validator.ValidateAsync(new ConvertCurrencyCommand("EUR", []));
        empty.Errors.Should().Contain(error => error.ErrorMessage.Contains("At least one conversion item"));

        var oversized = await validator.ValidateAsync(new ConvertCurrencyCommand(
            "EUR",
            Enumerable.Range(0, 51).Select(_ => new CurrencyConversionItemInput(1m, "USD")).ToList()));
        oversized.Errors.Should().Contain(error => error.ErrorMessage.Contains("At most 50"));

        var future = await validator.ValidateAsync(new ConvertCurrencyCommand(
            "EUR",
            [new CurrencyConversionItemInput(1m, "USD")],
            AsOfUtc: SystemClock.UtcNow.AddDays(2)));
        future.Errors.Should().Contain(error => error.ErrorMessage.Contains("cannot be in the future"));
    }

    private Task<CurrencyConversionResult> HandleAsync(
        CurrencyFxTestDbContext db,
        IExchangeRateProvider rates,
        ConvertCurrencyCommand query)
        => new ConvertCurrencyCommandHandler(db, rates, _actorAccessor.Object).Handle(query, CancellationToken.None);

    private async Task EnableAsync(CurrencyFxTestDbContext db, params string[] codes)
    {
        foreach (var code in codes)
        {
            await db.Set<CurrencyConfiguration>().AddAsync(new CurrencyConfiguration
            {
                TenantId = _actor.TenantId!.Value,
                CurrencyCode = code,
                IsEnabled = true,
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task AddSnapshotAsync(
        CurrencyFxTestDbContext db,
        string baseCurrency,
        string quoteCurrency,
        decimal rate,
        DateTime effectiveDate)
    {
        await db.Set<FxRateSnapshot>().AddAsync(new FxRateSnapshot
        {
            BaseCurrency = baseCurrency,
            QuoteCurrency = quoteCurrency,
            Rate = rate,
            EffectiveDateUtc = effectiveDate,
            AsOfUtc = effectiveDate,
            ProviderName = "test-provider",
            Source = "test",
        });
        await db.SaveChangesAsync();
    }

    private CurrencyFxTestDbContext NewDatabase()
    {
        var options = new DbContextOptionsBuilder<CurrencyFxTestDbContext>()
            .UseInMemoryDatabase($"currency-conversion-{Guid.NewGuid():N}")
            .Options;
        return new CurrencyFxTestDbContext(options);
    }

    private static ActorContext AuthenticatedActor() => new()
    {
        ActorKind = ActorKind.User,
        SubjectId = Guid.NewGuid().ToString(),
        TenantId = Guid.NewGuid(),
        Roles = new HashSet<string> { "TenantAdmin" },
        Permissions = new HashSet<string>(),
        TypedAttributes = ActorAttributes.Empty,
        AuthScheme = "Test",
        IsAuthenticated = true
    };
}
