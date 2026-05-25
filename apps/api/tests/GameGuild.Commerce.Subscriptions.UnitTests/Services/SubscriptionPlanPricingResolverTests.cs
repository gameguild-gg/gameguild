using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Services;

public class SubscriptionPlanPricingResolverTests
{
    private readonly Mock<ISubscriptionPlanService> _planService = new();
    private readonly SubscriptionPlanPricingResolver _resolver;

    public SubscriptionPlanPricingResolverTests()
    {
        _resolver = new SubscriptionPlanPricingResolver(_planService.Object);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenPlanServiceIsNull()
    {
        var act = () => new SubscriptionPlanPricingResolver(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("planService");
    }

    [Fact]
    public async Task GetPlanMonthlyPriceAsync_ShouldReturnNull_WhenPlanDoesNotExist()
    {
        _planService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        var result = await _resolver.GetPlanMonthlyPriceAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPlanMonthlyPriceAsync_ShouldReturnMonthlyPrice_WhenPlanExists()
    {
        _planService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePlan());

        var result = await _resolver.GetPlanMonthlyPriceAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result!.Amount.Should().Be(19.99m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task GetPlanPriceAsync_ShouldReturnNull_WhenPlanDoesNotExist()
    {
        _planService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        var result = await _resolver.GetPlanPriceAsync(Guid.NewGuid(), BillingCycle.Monthly);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPlanPriceAsync_ShouldUseAnnualPrice_WhenConfigured()
    {
        var plan = CreatePlan();
        plan.AnnualPriceInCents = 19990;
        _planService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var result = await _resolver.GetPlanPriceAsync(Guid.NewGuid(), BillingCycle.Annually);

        result!.Amount.Should().Be(199.90m);
    }

    [Fact]
    public async Task GetPlanPriceAsync_ShouldFallbackToTwelveMonths_WhenAnnualPriceMissing()
    {
        _planService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePlan());

        var result = await _resolver.GetPlanPriceAsync(Guid.NewGuid(), BillingCycle.Annually);

        result!.Amount.Should().Be(239.88m);
    }

    [Theory]
    [InlineData(BillingCycle.SemiAnnually, 119.94)]
    [InlineData(BillingCycle.Quarterly, 59.97)]
    [InlineData(BillingCycle.Monthly, 19.99)]
    [InlineData(BillingCycle.Weekly, 4.99)]
    [InlineData(BillingCycle.Biannually, 479.76)]
    public async Task GetPlanPriceAsync_ShouldMapBillingCyclesToExpectedAmounts(BillingCycle cycle, decimal expectedAmount)
    {
        _planService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePlan());

        var result = await _resolver.GetPlanPriceAsync(Guid.NewGuid(), cycle);

        result!.Amount.Should().Be(expectedAmount);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task GetPlanPriceAsync_ShouldFallbackToMonthly_ForUnsupportedCycle()
    {
        _planService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePlan());

        var result = await _resolver.GetPlanPriceAsync(Guid.NewGuid(), (BillingCycle)999);

        result!.Amount.Should().Be(19.99m);
    }

    [Fact]
    public async Task PlanExistsAsync_ShouldReturnExpectedExistence()
    {
        _planService.SetupSequence(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null)
            .ReturnsAsync(CreatePlan());

        var missing = await _resolver.PlanExistsAsync(Guid.NewGuid());
        var existing = await _resolver.PlanExistsAsync(Guid.NewGuid());

        missing.Should().BeFalse();
        existing.Should().BeTrue();
    }

    private static SubscriptionPlan CreatePlan()
    {
        var plan = new SubscriptionPlan("Pro", "pro", 1999, "USD", "Professional plan");
        typeof(SubscriptionPlan).GetProperty(nameof(SubscriptionPlan.Id))!.SetValue(plan, Guid.NewGuid());
        return plan;
    }
}
