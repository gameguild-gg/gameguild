using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Products.UnitTests.Entities;

public class PricingTierTests
{
    [Fact]
    public void AppliesToQuantity_ShouldReturnTrue_WhenInRange()
    {
        var tier = new PricingTier { IsActive = true, MinQuantity = 5, MaxQuantity = 10, UnitPrice = 9.99m };
        tier.AppliesToQuantity(7).Should().BeTrue();
    }

    [Fact]
    public void AppliesToQuantity_ShouldReturnTrue_AtBoundaries()
    {
        var tier = new PricingTier { IsActive = true, MinQuantity = 5, MaxQuantity = 10 };
        tier.AppliesToQuantity(5).Should().BeTrue();
        tier.AppliesToQuantity(10).Should().BeTrue();
    }

    [Fact]
    public void AppliesToQuantity_ShouldReturnFalse_WhenBelowMin()
    {
        var tier = new PricingTier { IsActive = true, MinQuantity = 5, MaxQuantity = 10 };
        tier.AppliesToQuantity(4).Should().BeFalse();
    }

    [Fact]
    public void AppliesToQuantity_ShouldReturnFalse_WhenAboveMax()
    {
        var tier = new PricingTier { IsActive = true, MinQuantity = 5, MaxQuantity = 10 };
        tier.AppliesToQuantity(11).Should().BeFalse();
    }

    [Fact]
    public void AppliesToQuantity_ShouldReturnTrue_WhenNoMaxQuantity()
    {
        var tier = new PricingTier { IsActive = true, MinQuantity = 5, MaxQuantity = null };
        tier.AppliesToQuantity(999).Should().BeTrue();
    }

    [Fact]
    public void AppliesToQuantity_ShouldReturnFalse_WhenInactive()
    {
        var tier = new PricingTier { IsActive = false, MinQuantity = 1, MaxQuantity = 100 };
        tier.AppliesToQuantity(5).Should().BeFalse();
    }

    [Fact]
    public void CalculateTotalPrice_ShouldMultiplyByQuantity()
    {
        var tier = new PricingTier { IsActive = true, MinQuantity = 1, MaxQuantity = 100, UnitPrice = 10m };
        tier.CalculateTotalPrice(5).Should().Be(50m);
    }

    [Fact]
    public void CalculateTotalPrice_ShouldReturnZero_WhenNotApplicable()
    {
        var tier = new PricingTier { IsActive = true, MinQuantity = 10, UnitPrice = 10m };
        tier.CalculateTotalPrice(5).Should().Be(0m);
    }
}
