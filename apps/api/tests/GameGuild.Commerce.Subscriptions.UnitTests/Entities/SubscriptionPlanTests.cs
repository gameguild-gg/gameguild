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
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange
        var name = "Enterprise Plan";
        var slug = "enterprise";
        var price = 9999L;

        // Act
        var plan = new TestSubscriptionPlan(name, slug, price);

        // Assert - Plan should be created with correct defaults
        plan.IsActive.Should().BeTrue();
        plan.IsFeatured.Should().BeFalse();
        plan.SortOrder.Should().Be(0);
        plan.TrialPeriodDays.Should().Be(0);
        plan.MaxUsers.Should().BeNull();
        plan.MaxStorageMb.Should().BeNull();
        plan.MaxApiCallsPerMonth.Should().BeNull();
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

    #region Money Value Object Tests

    [Fact]
    public void GetMonthlyPrice_ShouldReturnCorrectMoneyObject()
    {
        // Arrange
        var plan = new TestSubscriptionPlan("Pro", "pro", 2999);

        // Act
        var price = plan.GetMonthlyPrice();

        // Assert
        price.Amount.Should().Be(29.99m);
        price.Currency.Should().Be("USD");
    }

    [Fact]
    public void GetAnnualPrice_ShouldReturnNull_WhenNotSet()
    {
        // Arrange
        var plan = new TestSubscriptionPlan("Pro", "pro", 2999);

        // Act
        var price = plan.GetAnnualPrice();

        // Assert
        price.Should().BeNull();
    }

    [Fact]
    public void GetAnnualPrice_ShouldReturnMoneyObject_WhenSet()
    {
        // Arrange
        var plan = new TestSubscriptionPlan("Pro", "pro", 2999);
        plan.AnnualPriceInCents = 29999;

        // Act
        var price = plan.GetAnnualPrice();

        // Assert
        price.Should().NotBeNull();
        price!.Amount.Should().Be(299.99m);
    }

    #endregion

    #region Limits Validation Tests

    [Fact]
    public void AllowsUserCount_ShouldReturnTrue_WhenNoLimit()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.UpdateLimits(maxUsers: null);

        // Act & Assert
        plan.AllowsUserCount(1000).Should().BeTrue();
    }

    [Fact]
    public void AllowsUserCount_ShouldReturnTrue_WhenWithinLimit()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.UpdateLimits(maxUsers: 50);

        // Act & Assert
        plan.AllowsUserCount(25).Should().BeTrue();
        plan.AllowsUserCount(50).Should().BeTrue();
    }

    [Fact]
    public void AllowsUserCount_ShouldReturnFalse_WhenExceedsLimit()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.UpdateLimits(maxUsers: 50);

        // Act & Assert
        plan.AllowsUserCount(51).Should().BeFalse();
    }

    [Fact]
    public void AllowsStorage_ShouldReturnTrue_WhenNoLimit()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.UpdateLimits(maxStorageMb: null);

        // Act & Assert
        plan.AllowsStorage(1000000).Should().BeTrue();
    }

    [Fact]
    public void AllowsStorage_ShouldReturnTrue_WhenWithinLimit()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.UpdateLimits(maxStorageMb: 10240); // 10GB

        // Act & Assert
        plan.AllowsStorage(5000).Should().BeTrue();
        plan.AllowsStorage(10240).Should().BeTrue();
    }

    [Fact]
    public void AllowsStorage_ShouldReturnFalse_WhenExceedsLimit()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.UpdateLimits(maxStorageMb: 10240);

        // Act & Assert
        plan.AllowsStorage(10241).Should().BeFalse();
    }

    [Fact]
    public void AllowsApiCalls_ShouldReturnTrue_WhenNoLimit()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.UpdateLimits(maxApiCallsPerMonth: null);

        // Act & Assert
        plan.AllowsApiCalls(1000000).Should().BeTrue();
    }

    [Fact]
    public void AllowsApiCalls_ShouldReturnTrue_WhenWithinLimit()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.UpdateLimits(maxApiCallsPerMonth: 100000);

        // Act & Assert
        plan.AllowsApiCalls(50000).Should().BeTrue();
        plan.AllowsApiCalls(100000).Should().BeTrue();
    }

    [Fact]
    public void AllowsApiCalls_ShouldReturnFalse_WhenExceedsLimit()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.UpdateLimits(maxApiCallsPerMonth: 100000);

        // Act & Assert
        plan.AllowsApiCalls(100001).Should().BeFalse();
    }

    #endregion

    #region Update Methods Tests

    [Fact]
    public void UpdateDetails_ShouldUpdateNameAndDescription()
    {
        // Arrange
        var plan = CreateValidPlan();
        var newName = "Updated Plan";
        var newDescription = "Updated description";

        // Act
        plan.UpdateDetails(newName, newDescription, sortOrder: 5);

        // Assert
        plan.Name.Should().Be(newName);
        plan.Description.Should().Be(newDescription);
        plan.SortOrder.Should().Be(5);
    }

    [Fact]
    public void UpdateDetails_ShouldRaiseDomainEvent()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.ClearDomainEvents();

        // Act
        plan.UpdateDetails("New Name");

        // Assert
        plan.DomainEvents.Should().Contain(e => e.GetType() == typeof(PlanModifiedEvent));
    }

    [Fact]
    public void UpdatePricing_ShouldUpdateMonthlyAndAnnualPrice()
    {
        // Arrange
        var plan = CreateValidPlan();

        // Act
        plan.UpdatePricing(4999, 49999);

        // Assert
        plan.MonthlyPriceInCents.Should().Be(4999);
        plan.AnnualPriceInCents.Should().Be(49999);
    }

    [Fact]
    public void UpdateLimits_ShouldUpdateAllLimits()
    {
        // Arrange
        var plan = CreateValidPlan();

        // Act
        plan.UpdateLimits(maxUsers: 100, maxStorageMb: 50000, maxApiCallsPerMonth: 1000000);

        // Assert
        plan.MaxUsers.Should().Be(100);
        plan.MaxStorageMb.Should().Be(50000);
        plan.MaxApiCallsPerMonth.Should().Be(1000000);
    }

    [Fact]
    public void UpdateFeatures_ShouldUpdateFeatureFlags()
    {
        // Arrange
        var plan = CreateValidPlan();

        // Act
        plan.UpdateFeatures(
            hasPrioritySupport: true,
            hasAdvancedAnalytics: true,
            hasCustomBranding: true,
            features: "[\"api_access\",\"webhooks\"]"
        );

        // Assert
        plan.HasPrioritySupport.Should().BeTrue();
        plan.HasAdvancedAnalytics.Should().BeTrue();
        plan.HasCustomBranding.Should().BeTrue();
        plan.Features.Should().Be("[\"api_access\",\"webhooks\"]");
    }

    [Fact]
    public void UpdateFeatures_ShouldOnlyUpdateProvidedValues()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.HasPrioritySupport = true;
        plan.HasAdvancedAnalytics = false;

        // Act - only update analytics
        plan.UpdateFeatures(hasAdvancedAnalytics: true);

        // Assert
        plan.HasPrioritySupport.Should().BeTrue(); // Unchanged
        plan.HasAdvancedAnalytics.Should().BeTrue(); // Updated
        plan.HasCustomBranding.Should().BeFalse(); // Unchanged
    }

    #endregion

    #region Activation/Deactivation Tests

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.Deactivate();

        // Act
        plan.Activate();

        // Assert
        plan.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_ShouldBeIdempotent()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.IsActive.Should().BeTrue();

        // Act - should not throw
        var act = () => plan.Activate();

        // Assert
        act.Should().NotThrow();
        plan.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var plan = CreateValidPlan();

        // Act
        plan.Deactivate();

        // Assert
        plan.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ShouldRaiseDomainEvent()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.ClearDomainEvents();

        // Act
        plan.Deactivate();

        // Assert
        plan.DomainEvents.Should().Contain(e => e.GetType() == typeof(PlanDiscontinuedEvent));
    }

    [Fact]
    public void Deactivate_ShouldBeIdempotent()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.Deactivate();
        plan.ClearDomainEvents();

        // Act - second deactivation should not raise event
        plan.Deactivate();

        // Assert
        plan.DomainEvents.Should().BeEmpty(); // No new event raised
        plan.IsActive.Should().BeFalse();
    }

    #endregion

    #region Featured and External ID Tests

    [Fact]
    public void SetFeatured_ShouldSetFeaturedFlag()
    {
        // Arrange
        var plan = CreateValidPlan();

        // Act
        plan.SetFeatured(true);

        // Assert
        plan.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public void SetFeatured_WithFalse_ShouldClearFeaturedFlag()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.SetFeatured(true);

        // Act
        plan.SetFeatured(false);

        // Assert
        plan.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void SetExternalId_ShouldSetExternalId()
    {
        // Arrange
        var plan = CreateValidPlan();

        // Act
        plan.SetExternalId("price_abc123");

        // Assert
        plan.ExternalId.Should().Be("price_abc123");
    }

    #endregion

    #region Annual Savings Calculation Tests

    [Fact]
    public void CalculateAnnualSavingsPercentage_ShouldReturnZero_WhenNoAnnualPrice()
    {
        // Arrange
        var plan = CreateValidPlan();
        plan.AnnualPriceInCents = null;

        // Act
        var savings = plan.CalculateAnnualSavingsPercentage();

        // Assert
        savings.Should().Be(0);
    }

    [Fact]
    public void CalculateAnnualSavingsPercentage_ShouldCalculateCorrectly()
    {
        // Arrange
        var plan = new TestSubscriptionPlan("Pro", "pro", 1000); // $10/month
        plan.AnnualPriceInCents = 10000; // $100/year (12 months = $120, savings = $20)

        // Act
        var savings = plan.CalculateAnnualSavingsPercentage();

        // Assert
        // (12000 - 10000) / 12000 * 100 = 16.67%
        savings.Should().BeApproximately(16.67m, 0.01m);
    }

    [Fact]
    public void CalculateAnnualSavingsPercentage_ShouldReturnZero_WhenMonthlyPriceIsZero()
    {
        // Arrange
        var plan = new TestSubscriptionPlan("Free", "free", 0);
        plan.AnnualPriceInCents = 0;

        // Act
        var savings = plan.CalculateAnnualSavingsPercentage();

        // Assert
        savings.Should().Be(0);
    }

    [Fact]
    public void CalculateAnnualSavingsPercentage_WithNoSavings_ShouldReturnZero()
    {
        // Arrange
        var plan = new TestSubscriptionPlan("Pro", "pro", 1000);
        plan.AnnualPriceInCents = 12000; // Same as 12 months

        // Act
        var savings = plan.CalculateAnnualSavingsPercentage();

        // Assert
        savings.Should().Be(0);
    }

    #endregion

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
