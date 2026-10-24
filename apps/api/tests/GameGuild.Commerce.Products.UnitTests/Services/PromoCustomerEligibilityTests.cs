using FluentAssertions;
using GameGuild.Commerce.Products;
using Moq;
using Xunit;

namespace GameGuild.Tests.Commerce.Products.Unit.Services;

/// <summary>
/// Issue #418 customer-type eligibility: the resolver classifies the customer's
/// relationship state from commerce signals, and the pricing engine rejects codes
/// whose eligibility restriction does not match.
/// </summary>
public class PromoCustomerEligibilityTests
{
    private readonly Mock<IUserProductRepository> _userProductRepository = new();
    private readonly Mock<IPromoCodeRepository> _promoCodeRepository = new();

    private PromoCustomerStateResolver CreateResolver() =>
        new(_userProductRepository.Object, _promoCodeRepository.Object);

    private static UserProduct Purchase(bool active)
    {
        var purchase = new UserProduct
        {
            UserId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            AccessStatus = ProductAccessStatus.Active,
            PricePaid = 10m
        };
        if (!active)
        {
            purchase.RevokeAccess("test");
        }

        return purchase;
    }

    [Fact]
    public async Task Resolve_Anonymous_IsNew()
    {
        var state = await CreateResolver().ResolveAsync(null);

        state.Should().Be(PromoCustomerType.New);
    }

    [Fact]
    public async Task Resolve_NoPurchasesAndNoPromoHistory_IsNew()
    {
        var userId = Guid.NewGuid();
        _userProductRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<UserProduct>());
        _promoCodeRepository.Setup(r => r.HasUserRecordedUsageAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var state = await CreateResolver().ResolveAsync(userId);

        state.Should().Be(PromoCustomerType.New);
    }

    [Fact]
    public async Task Resolve_ActivePurchase_IsExisting()
    {
        var userId = Guid.NewGuid();
        _userProductRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Purchase(active: true) });
        _promoCodeRepository.Setup(r => r.HasUserRecordedUsageAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var state = await CreateResolver().ResolveAsync(userId);

        state.Should().Be(PromoCustomerType.Existing);
    }

    [Fact]
    public async Task Resolve_OnlyExpiredPurchases_IsChurned()
    {
        var userId = Guid.NewGuid();
        _userProductRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Purchase(active: false) });

        var state = await CreateResolver().ResolveAsync(userId);

        state.Should().Be(PromoCustomerType.Churned);
    }

    [Fact]
    public async Task Resolve_PromoHistoryWithoutPurchases_IsExisting()
    {
        var userId = Guid.NewGuid();
        _userProductRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<UserProduct>());
        _promoCodeRepository.Setup(r => r.HasUserRecordedUsageAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var state = await CreateResolver().ResolveAsync(userId);

        state.Should().Be(PromoCustomerType.Existing);
    }

    [Fact]
    public async Task Validate_NewOnlyCode_RejectsExistingCustomer()
    {
        var userId = Guid.NewGuid();
        var code = EligibleCode(PromoCustomerType.New);
        var engine = CreateEngine(PromoCustomerType.Existing);
        _promoCodeRepository.Setup(r => r.GetByCodeAsync(code.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(code);

        var result = await engine.ValidatePromoCodeAsync(code.Code, 100m, userId: userId);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("New");
        result.Outcome.Should().Be(PromoCodeValidationOutcome.CustomerNotEligible);
    }

    [Fact]
    public async Task Validate_NewOnlyCode_AcceptsFirstTimeCustomer()
    {
        var userId = Guid.NewGuid();
        var code = EligibleCode(PromoCustomerType.New);
        var engine = CreateEngine(PromoCustomerType.New);
        _promoCodeRepository.Setup(r => r.GetByCodeAsync(code.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(code);

        var result = await engine.ValidatePromoCodeAsync(code.Code, 100m, userId: userId);

        result.IsValid.Should().BeTrue();
        result.Outcome.Should().Be(PromoCodeValidationOutcome.Valid);
    }

    [Fact]
    public async Task Validate_ChurnedOnlyCode_RejectsActiveCustomer()
    {
        var userId = Guid.NewGuid();
        var code = EligibleCode(PromoCustomerType.Churned);
        var engine = CreateEngine(PromoCustomerType.Existing);
        _promoCodeRepository.Setup(r => r.GetByCodeAsync(code.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(code);

        var result = await engine.ValidatePromoCodeAsync(code.Code, 100m, userId: userId);

        result.IsValid.Should().BeFalse();
        result.Outcome.Should().Be(PromoCodeValidationOutcome.CustomerNotEligible);
    }

    [Fact]
    public async Task Validate_ChurnedOnlyCode_AcceptsChurnedCustomer()
    {
        var userId = Guid.NewGuid();
        var code = EligibleCode(PromoCustomerType.Churned);
        var engine = CreateEngine(PromoCustomerType.Churned);
        _promoCodeRepository.Setup(r => r.GetByCodeAsync(code.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(code);

        var result = await engine.ValidatePromoCodeAsync(code.Code, 100m, userId: userId);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_AnyCode_AcceptsEveryCustomerState()
    {
        var code = EligibleCode(PromoCustomerType.Any);
        _promoCodeRepository.Setup(r => r.GetByCodeAsync(code.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(code);

        foreach (var state in new[] { PromoCustomerType.New, PromoCustomerType.Existing, PromoCustomerType.Churned })
        {
            var engine = CreateEngine(state);
            var result = await engine.ValidatePromoCodeAsync(code.Code, 100m, userId: Guid.NewGuid());
            result.IsValid.Should().BeTrue($"Any-restricted code must accept a {state} customer");
        }
    }

    [Fact]
    public async Task Validate_RestrictedCode_WithoutResolver_FailsClosed()
    {
        var code = EligibleCode(PromoCustomerType.New);
        _promoCodeRepository.Setup(r => r.GetByCodeAsync(code.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(code);
        var engine = new PricingEngineService(
            Mock.Of<IProductRepository>(),
            _promoCodeRepository.Object,
            Mock.Of<IPromoStackingRuleRepository>(),
            Mock.Of<IPricingRuleRepository>(),
            Mock.Of<IPricingExperimentService>(),
            new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
            customerStateResolver: null);

        var result = await engine.ValidatePromoCodeAsync(code.Code, 100m, userId: Guid.NewGuid());

        result.IsValid.Should().BeFalse();
        result.Outcome.Should().Be(PromoCodeValidationOutcome.CustomerNotEligible);
    }

    [Fact]
    public void IsEligible_AnyAcceptsEverything_StrictMatchOtherwise()
    {
        PromoCustomerEligibility.IsEligible(PromoCustomerType.Any, PromoCustomerType.Churned).Should().BeTrue();
        PromoCustomerEligibility.IsEligible(PromoCustomerType.New, PromoCustomerType.New).Should().BeTrue();
        PromoCustomerEligibility.IsEligible(PromoCustomerType.New, PromoCustomerType.Existing).Should().BeFalse();
        PromoCustomerEligibility.IsEligible(PromoCustomerType.Existing, PromoCustomerType.Churned).Should().BeFalse();
    }

    private static PromoCode EligibleCode(PromoCustomerType eligibility)
    {
        return new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = $"ELIG{(int)eligibility}",
            Name = "Eligibility-scoped code",
            Type = PromoCodeType.PercentageOff,
            DiscountPercentage = 10m,
            Currency = "USD",
            IsActive = true,
            EligibleCustomerType = eligibility
        };
    }

    private PricingEngineService CreateEngine(PromoCustomerType resolvedState)
    {
        var resolver = new Mock<IPromoCustomerStateResolver>();
        resolver.Setup(r => r.ResolveAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedState);

        return new PricingEngineService(
            Mock.Of<IProductRepository>(),
            _promoCodeRepository.Object,
            Mock.Of<IPromoStackingRuleRepository>(),
            Mock.Of<IPricingRuleRepository>(),
            Mock.Of<IPricingExperimentService>(),
            new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
            resolver.Object);
    }
}
