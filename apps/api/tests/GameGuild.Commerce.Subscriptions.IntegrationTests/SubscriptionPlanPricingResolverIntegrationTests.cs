using FluentAssertions;
using GameGuild.Commerce.Subscriptions;

using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.IntegrationTests;

/// <summary>
///     Integration tests for SubscriptionPlanPricingResolver
///     Tests the cross-module integration between Subscriptions and Payments modules
/// </summary>
public class SubscriptionPlanPricingResolverIntegrationTests
{
    private readonly Mock<ISubscriptionPlanService> _mockPlanService;
    private readonly SubscriptionPlanPricingResolver _resolver;

    public SubscriptionPlanPricingResolverIntegrationTests()
    {
        _mockPlanService = new Mock<ISubscriptionPlanService>();
        _resolver = new SubscriptionPlanPricingResolver(_mockPlanService.Object);
    }

    #region GetPlanMonthlyPriceAsync Tests

    [Fact]
    public async Task GetPlanMonthlyPriceAsync_ShouldReturnMoney_WhenPlanExists()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Pro Plan", "pro-plan", 2999, "USD");

        _mockPlanService.Setup(s => s.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _resolver.GetPlanMonthlyPriceAsync(planId);

        // Assert
        result.Should().NotBeNull();
        result!.Amount.Should().Be(29.99m); // 2999 cents = $29.99
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task GetPlanMonthlyPriceAsync_ShouldReturnNull_WhenPlanNotFound()
    {
        // Arrange
        var planId = Guid.NewGuid();

        _mockPlanService.Setup(s => s.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        // Act
        var result = await _resolver.GetPlanMonthlyPriceAsync(planId);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(999, "USD", 9.99)]
    [InlineData(4999, "USD", 49.99)]
    [InlineData(1000, "EUR", 10.00)]
    public async Task GetPlanMonthlyPriceAsync_ShouldConvertCentsToMoney_ForDifferentPrices(
        long priceInCents, string currency, decimal expectedAmount)
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Plan", "plan", priceInCents, currency);

        _mockPlanService.Setup(s => s.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _resolver.GetPlanMonthlyPriceAsync(planId);

        // Assert
        result.Should().NotBeNull();
        result!.Amount.Should().Be(expectedAmount);
        result.Currency.Should().Be(currency);
    }

    #endregion

    #region GetPlanPriceAsync Tests

    [Theory]
    [InlineData(BillingCycle.Monthly, 2999, 29.99)]
    [InlineData(BillingCycle.Quarterly, 2999, 89.97)] // 3 months
    [InlineData(BillingCycle.SemiAnnually, 2999, 179.94)] // 6 months
    public async Task GetPlanPriceAsync_ShouldCalculatePrice_ForDifferentBillingCycles(
        BillingCycle billingCycle, long monthlyPriceInCents, decimal expectedAmount)
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Plan", "plan", monthlyPriceInCents, "USD");

        _mockPlanService.Setup(s => s.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _resolver.GetPlanPriceAsync(planId, billingCycle);

        // Assert
        result.Should().NotBeNull();
        result!.Amount.Should().Be(expectedAmount);
    }

    [Fact]
    public async Task GetPlanPriceAsync_ShouldUseAnnualPrice_WhenProvided()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Plan", "plan", 2999, "USD");
        plan.UpdatePricing(2999, 29990); // Annual price with discount (normally 35988)

        _mockPlanService.Setup(s => s.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _resolver.GetPlanPriceAsync(planId, BillingCycle.Annually);

        // Assert
        result.Should().NotBeNull();
        result!.Amount.Should().Be(299.90m); // Uses explicit annual price
    }

    [Fact]
    public async Task GetPlanPriceAsync_ShouldFallbackToMonthlyMultiplier_WhenNoAnnualPrice()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Plan", "plan", 2999, "USD");
        // No annual price set

        _mockPlanService.Setup(s => s.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _resolver.GetPlanPriceAsync(planId, BillingCycle.Annually);

        // Assert
        result.Should().NotBeNull();
        result!.Amount.Should().Be(359.88m); // 29.99 * 12
    }

    [Fact]
    public async Task GetPlanPriceAsync_ShouldReturnNull_WhenPlanNotFound()
    {
        // Arrange
        var planId = Guid.NewGuid();

        _mockPlanService.Setup(s => s.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        // Act
        var result = await _resolver.GetPlanPriceAsync(planId, BillingCycle.Monthly);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region PlanExistsAsync Tests

    [Fact]
    public async Task PlanExistsAsync_ShouldReturnTrue_WhenPlanExists()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Plan", "plan", 999);

        _mockPlanService.Setup(s => s.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _resolver.PlanExistsAsync(planId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PlanExistsAsync_ShouldReturnFalse_WhenPlanNotFound()
    {
        // Arrange
        var planId = Guid.NewGuid();

        _mockPlanService.Setup(s => s.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        // Act
        var result = await _resolver.PlanExistsAsync(planId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldThrow_WhenPlanServiceIsNull()
    {
        // Act
        var act = () => new SubscriptionPlanPricingResolver(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("planService");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetPlanPriceAsync_ShouldHandleWeeklyBillingCycle()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Plan", "plan", 4000, "USD"); // $40/month

        _mockPlanService.Setup(s => s.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _resolver.GetPlanPriceAsync(planId, BillingCycle.Weekly);

        // Assert
        result.Should().NotBeNull();
        result!.Amount.Should().Be(10.00m); // 4000/4 = 1000 cents = $10
    }

    [Fact]
    public async Task GetPlanPriceAsync_ShouldHandleBiannuallyBillingCycle()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan("Plan", "plan", 1000, "USD"); // $10/month

        _mockPlanService.Setup(s => s.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _resolver.GetPlanPriceAsync(planId, BillingCycle.Biannually);

        // Assert
        result.Should().NotBeNull();
        result!.Amount.Should().Be(240.00m); // $10 * 24 months
    }

    #endregion
}
