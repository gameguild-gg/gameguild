using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.UnitTests.Entities;

public class PricingRuleTests
{
    [Fact]
    public void IsApplicable_ShouldReturnFalse_WhenInactive()
    {
        var rule = new PricingRule { IsActive = false };

        rule.IsApplicable().Should().BeFalse();
    }

    [Fact]
    public void IsApplicable_ShouldRespect_Start_And_End_Dates()
    {
        var now = DateTime.UtcNow;
        var rule = new PricingRule
        {
            IsActive = true,
            StartDate = now.AddHours(1),
            EndDate = now.AddHours(2)
        };

        rule.IsApplicable(now).Should().BeFalse();
        rule.IsApplicable(now.AddHours(1).AddMinutes(1)).Should().BeTrue();
        rule.IsApplicable(now.AddHours(3)).Should().BeFalse();
    }

    [Fact]
    public void IsApplicable_WithQuantity_ShouldRespect_Min_And_Max()
    {
        var now = DateTime.UtcNow;
        var rule = new PricingRule
        {
            IsActive = true,
            StartDate = now.AddHours(-1),
            EndDate = now.AddHours(1),
            MinQuantity = 2,
            MaxQuantity = 5
        };

        rule.IsApplicable(1, now).Should().BeFalse();
        rule.IsApplicable(2, now).Should().BeTrue();
        rule.IsApplicable(5, now).Should().BeTrue();
        rule.IsApplicable(6, now).Should().BeFalse();
    }

    [Fact]
    public void AppliesToQuantity_ShouldReturnFalse_WhenNotApplicable()
    {
        var rule = new PricingRule
        {
            IsActive = false,
            MinQuantity = 1,
            MaxQuantity = 10
        };

        rule.AppliesToQuantity(5).Should().BeFalse();
    }

    [Fact]
    public void CalculatePrice_ShouldApply_VolumeDiscount()
    {
        var rule = new PricingRule
        {
            IsActive = true,
            RuleType = PricingRuleType.VolumeDiscount,
            DiscountPercentage = 10,
            MinQuantity = 1
        };

        var result = rule.CalculatePrice(100m, 2);

        result.Should().Be(90m);
    }

    [Fact]
    public void CalculatePrice_ShouldApply_FixedPriceOverride()
    {
        var rule = new PricingRule
        {
            IsActive = true,
            RuleType = PricingRuleType.FixedPriceOverride,
            FixedPrice = 42m
        };

        rule.CalculatePrice(100m, 1).Should().Be(42m);
    }

    [Fact]
    public void CalculatePrice_ShouldApply_RegionBased_FixedPrice_WhenProvided()
    {
        var rule = new PricingRule
        {
            IsActive = true,
            RuleType = PricingRuleType.RegionBased,
            FixedPrice = 70m
        };

        rule.CalculatePrice(100m, 1).Should().Be(70m);
    }

    [Fact]
    public void CalculatePrice_ShouldApply_RegionBased_Discount_WhenNoFixedPrice()
    {
        var rule = new PricingRule
        {
            IsActive = true,
            RuleType = PricingRuleType.RegionBased,
            DiscountPercentage = 25
        };

        rule.CalculatePrice(100m, 1).Should().Be(75m);
    }

    [Fact]
    public void CalculatePrice_ShouldApply_TimeBased_Discount()
    {
        var rule = new PricingRule
        {
            IsActive = true,
            RuleType = PricingRuleType.TimeBased,
            DiscountPercentage = 15
        };

        rule.CalculatePrice(200m, 1).Should().Be(170m);
    }

    [Fact]
    public void CalculatePrice_ShouldApply_SegmentBased_Discount()
    {
        var rule = new PricingRule
        {
            IsActive = true,
            RuleType = PricingRuleType.SegmentBased,
            DiscountPercentage = 20
        };

        rule.CalculatePrice(100m, 1).Should().Be(80m);
    }

    [Fact]
    public void CalculatePrice_Should_Return_BasePrice_For_MarketBased()
    {
        var rule = new PricingRule
        {
            IsActive = true,
            RuleType = PricingRuleType.MarketBased
        };

        rule.CalculatePrice(120m, 1).Should().Be(120m);
    }

    [Fact]
    public void CalculateDiscount_ShouldApply_Percentage_Discount()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.Percentage,
            DiscountPercentage = 10
        };

        rule.CalculateDiscount(50m, 2).Should().Be(10m);
    }

    [Fact]
    public void CalculateDiscount_ShouldApply_FixedAmount_Discount()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.FixedAmount,
            DiscountAmount = 5m
        };

        rule.CalculateDiscount(50m, 2).Should().Be(5m);
    }

    [Fact]
    public void CalculateDiscount_ShouldApply_BuyXGetY_Discount()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.BuyXGetY,
            BuyQuantity = 2,
            GetQuantity = 1
        };

        rule.CalculateDiscount(10m, 6).Should().Be(20m);
    }

    [Fact]
    public void CalculateDiscount_ShouldReturnZero_When_BuyXGetY_NotConfigured()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.BuyXGetY
        };

        rule.CalculateDiscount(10m, 6).Should().Be(0m);
    }

    [Fact]
    public void CalculateDiscount_ShouldApply_TieredPricing_ByPrice()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.TieredPricing,
            PricingTiers =
            {
                new PricingRuleTier { MinQuantity = 1, MaxQuantity = 10, Price = 8m }
            }
        };

        rule.CalculateDiscount(10m, 3).Should().Be(6m);
    }

    [Fact]
    public void CalculateDiscount_ShouldApply_TieredPricing_ByPercentage()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.TieredPricing,
            PricingTiers =
            {
                new PricingRuleTier { MinQuantity = 5, DiscountPercentage = 20 }
            }
        };

        rule.CalculateDiscount(10m, 6).Should().Be(12m);
    }

    [Fact]
    public void CalculateDiscount_ShouldReturnZero_When_TierHasNoPriceOrDiscount()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.TieredPricing,
            PricingTiers =
            {
                new PricingRuleTier { MinQuantity = 1, MaxQuantity = 10 }
            }
        };

        rule.CalculateDiscount(10m, 3).Should().Be(0m);
    }

    [Fact]
    public void CalculateDiscount_ShouldReturnZero_When_VolumeDiscount_HasNoTier()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.VolumeDiscount
        };

        rule.CalculateDiscount(10m, 5).Should().Be(0m);
    }

    [Fact]
    public void CalculateDiscount_ShouldReturnZero_WhenNoApplicableTier()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.TieredPricing
        };

        rule.CalculateDiscount(10m, 2).Should().Be(0m);
    }
}
