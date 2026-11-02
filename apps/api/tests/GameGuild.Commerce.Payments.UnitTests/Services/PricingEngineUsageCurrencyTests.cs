using FluentAssertions;
using GameGuild.Commerce;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Services;

/// <summary>
///     Usage-based charge currency conversion (issue #412 spec F): per-unit prices (flat and
///     progressive tiers) denominated in a currency other than the calculation currency are
///     converted through the FX core before the charge math, mirroring the add-on path;
///     unspecified currency keeps the historic behavior byte-for-byte.
/// </summary>
public sealed class PricingEngineUsageCurrencyTests
{
    private readonly Mock<IPlanPricingResolver> _planResolver = new();
    private readonly Mock<IPlanProrationCalculator> _prorationCalculator = new();
    private readonly Mock<IPromoDiscountCalculator> _promoCalculator = new();
    private readonly Mock<ITaxCalculationService> _taxService = new();
    private readonly Mock<IWalletService> _walletService = new();
    private readonly Mock<IPricingDecisionLedgerWriter> _ledgerWriter = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public PricingEngineUsageCurrencyTests()
    {
        _planResolver
            .Setup(x => x.GetPlanPriceAsync(It.IsAny<Guid>(), It.IsAny<BillingCycle>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Money(0m, "USD"));
    }

    private PricingEngine CreateEngine(IExchangeRateProvider rates)
    {
        var conversion = new CurrencyConversionService(rates);
        return new PricingEngine(
            _planResolver.Object,
            _prorationCalculator.Object,
            _promoCalculator.Object,
            _taxService.Object,
            _walletService.Object,
            conversion,
            _ledgerWriter.Object,
            _cache);
    }

    [Fact]
    public async Task UsageInDifferentCurrency_FlatPrice_IsConvertedToCalculationCurrency()
    {
        var rates = new ManualExchangeRateProvider(
        [
            new ExchangeRate("EUR", "USD", 1.1m, DateTimeOffset.UtcNow, "test")
        ]);

        // 10 EUR per unit × 5 units, calculation currency USD: 10 EUR → 11 USD per unit = 55 USD.
        var result = await CreateEngine(rates).CalculateAsync(new PricingCalculationRequest(
            BaseAmount: 0m,
            Currency: "USD",
            UsageCharges: [new PricingUsageChargeInput("api-calls", 5m, 10m, Currency: "EUR")],
            IncludeTax: false,
            RecordDecision: false));

        result.UsageCharges.Amount.Should().Be(55.00m);
        result.Currency.Should().Be("USD");
        var usageLine = result.LineItems.Single(item => item.Type == "usage");
        usageLine.UnitPrice.Should().Be(11m);
    }

    [Fact]
    public async Task UsageInDifferentCurrency_TieredPrices_AreConvertedBeforeProgressiveMath()
    {
        var rates = new ManualExchangeRateProvider(
        [
            new ExchangeRate("EUR", "USD", 1.1m, DateTimeOffset.UtcNow, "test")
        ]);

        // Bands: [0,10) at 2 EUR/unit, [10,null) at 1 EUR/unit; 20 units.
        // Converted: [0,10) at 2.2 USD, [10,∞) at 1.1 USD → 10×2.2 + 10×1.1 = 33 USD.
        var tiers = new List<PricingUsageTier>
        {
            new(0m, 10m, 2m),
            new(10m, null, 1m),
        };

        var result = await CreateEngine(rates).CalculateAsync(new PricingCalculationRequest(
            BaseAmount: 0m,
            Currency: "USD",
            UsageCharges: [new PricingUsageChargeInput("compute", 20m, 0m, tiers, Currency: "EUR")],
            IncludeTax: false,
            RecordDecision: false));

        result.UsageCharges.Amount.Should().Be(33.00m);
    }

    [Fact]
    public async Task UsageInSameCurrency_AsCalculationCurrency_IsNotConverted()
    {
        var rates = new ManualExchangeRateProvider(
        [
            new ExchangeRate("EUR", "USD", 1.1m, DateTimeOffset.UtcNow, "test")
        ]);

        // Explicit USD (equal to the calculation currency): unchanged historic math.
        var result = await CreateEngine(rates).CalculateAsync(new PricingCalculationRequest(
            BaseAmount: 0m,
            Currency: "USD",
            UsageCharges: [new PricingUsageChargeInput("api-calls", 5m, 10m, Currency: "USD")],
            IncludeTax: false,
            RecordDecision: false));

        result.UsageCharges.Amount.Should().Be(50m);
    }

    [Fact]
    public async Task UsageWithoutCurrency_KeepsHistoricBehavior()
    {
        // No currency supplied and no quotations registered at all: usage math must not
        // touch the FX seam (this is the pre-#412-F behavior).
        var result = await CreateEngine(new ManualExchangeRateProvider([])).CalculateAsync(new PricingCalculationRequest(
            BaseAmount: 0m,
            Currency: "USD",
            UsageCharges: [new PricingUsageChargeInput("api-calls", 5m, 10m)],
            IncludeTax: false,
            RecordDecision: false));

        result.UsageCharges.Amount.Should().Be(50m);
    }

    [Fact]
    public async Task UsageInDifferentCurrency_WithoutQuotation_FailsClosed()
    {
        var act = () => CreateEngine(new ManualExchangeRateProvider([])).CalculateAsync(new PricingCalculationRequest(
            BaseAmount: 0m,
            Currency: "USD",
            UsageCharges: [new PricingUsageChargeInput("api-calls", 5m, 10m, Currency: "EUR")],
            IncludeTax: false,
            RecordDecision: false));

        (await act.Should().ThrowAsync<BusinessRuleViolationException>())
            .Which.Rule.Should().Be(CurrencyConversionService.MissingRateRule);
    }
}
