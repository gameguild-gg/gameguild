using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Entities;

public class SubscriptionPlanTests
{
    // We'll use a concrete implementation for testing
    private class TestSubscriptionPlan : SubscriptionPlan
    {
        public TestSubscriptionPlan(string name, string slug, long monthlyPriceInCents, string currency = "USD", string? description = null)
            : base(name, slug, monthlyPriceInCents, currency, description)
        {
        }
    }

    [Fact]
    public void Constructor_ShouldCreatePlan_WithValidParameters()
    {
        // Arrange
        var name = "Professional Plan";
        var slug = "professional";
        var price = 2999L; // $29.99
        var currency = "USD";
        var description = "Perfect for growing teams";

        // Act
        var plan = new TestSubscriptionPlan(name, slug, price, currency, description);

        // Assert
        plan.Name.Should().Be(name);
        plan.Slug.Should().Be(slug);
        plan.MonthlyPriceInCents.Should().Be(price);
        plan.Currency.Should().Be(currency);
        plan.Description.Should().Be(description);
    }

    [Fact]
    public void Constructor_ShouldRaisePlanCreatedEvent()
    {
        // Arrange
        var name = "Enterprise Plan";
        var slug = "enterprise";
        var price = 9999L;

        // Act
        var plan = new TestSubscriptionPlan(name, slug, price);

        // Assert
        var events = plan.DomainEvents;
        events.Should().ContainSingle();
        events.First().Should().BeOfType<PlanCreatedEvent>();
        
        var createdEvent = events.First() as PlanCreatedEvent;
        createdEvent!.PlanId.Should().Be(plan.Id);
        createdEvent.Name.Should().Be(name);
        createdEvent.MonthlyPriceInCents.Should().Be(price);
    }

    [Fact]
    public void SetExternalId_ShouldUpdateExternalId()
    {
        // Arrange
        var plan = CreateValidPlan();
        var externalId = "price_1234567890";

        // Act
        plan.ExternalId = externalId;

        // Assert
        plan.ExternalId.Should().Be(externalId);
    }

    [Fact]
    public void SetFeatured_ShouldUpdateFeaturedFlag()
    {
        // Arrange
        var plan = CreateValidPlan();

        // Act
        plan.IsFeatured = true;

        // Assert
        plan.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public void SetTrialPeriod_ShouldUpdateTrialDays()
    {
        // Arrange
        var plan = CreateValidPlan();
        var trialDays = 30;

        // Act
        plan.TrialPeriodDays = trialDays;

        // Assert
        plan.TrialPeriodDays.Should().Be(trialDays);
    }

    [Fact]
    public void UpdateFeatures_ShouldStoreFeaturesList()
    {
        // Arrange
        var plan = CreateValidPlan();
        var features = "[\"feature1\",\"feature2\",\"feature3\"]";

        // Act
        plan.Features = features;

        // Assert
        plan.Features.Should().Be(features);
    }

    [Fact]
    public void Plan_WithDefaultCurrency_ShouldUseUSD()
    {
        // Arrange & Act
        var plan = new TestSubscriptionPlan("Basic", "basic", 999);

        // Assert
        plan.Currency.Should().Be("USD");
    }

    [Fact]
    public void Plan_WithCustomCurrency_ShouldStoreCurrency()
    {
        // Arrange & Act
        var plan = new TestSubscriptionPlan("Basic EUR", "basic-eur", 999, "EUR");

        // Assert
        plan.Currency.Should().Be("EUR");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    [InlineData(9999)]
    [InlineData(99999)]
    public void Plan_WithVariousPrices_ShouldStoreCorrectly(long price)
    {
        // Arrange & Act
        var plan = new TestSubscriptionPlan($"Plan {price}", $"plan-{price}", price);

        // Assert
        plan.MonthlyPriceInCents.Should().Be(price);
    }

    [Fact]
    public void Plan_WithoutDescription_ShouldAllowNullDescription()
    {
        // Arrange & Act
        var plan = new TestSubscriptionPlan("Simple Plan", "simple", 1999);

        // Assert
        plan.Description.Should().BeNull();
    }

    private static TestSubscriptionPlan CreateValidPlan()
    {
        return new TestSubscriptionPlan(
            "Test Plan",
            "test-plan",
            1999L,
            "USD",
            "A plan for testing"
        );
    }
}
