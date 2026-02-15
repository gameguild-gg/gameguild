using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.UnitTests.Entities;

public class PricingRuleEdgeCaseTests
{
    [Fact]
    public void IsApplicable_ShouldReturnTrue_WhenActiveWithNoDates()
    {
        var rule = new PricingRule { IsActive = true, StartDate = null, EndDate = null };

        rule.IsApplicable().Should().BeTrue();
    }

    [Fact]
    public void IsApplicable_ShouldReturnTrue_WhenActiveWithOnlyStartDateInPast()
    {
        var rule = new PricingRule
        {
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = null
        };

        rule.IsApplicable().Should().BeTrue();
    }

    [Fact]
    public void IsApplicable_ShouldReturnTrue_WhenActiveWithOnlyEndDateInFuture()
    {
        var rule = new PricingRule
        {
            IsActive = true,
            StartDate = null,
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        rule.IsApplicable().Should().BeTrue();
    }

    [Fact]
    public void IsApplicable_WithQuantity_ShouldReturnFalse_WhenInactive()
    {
        var rule = new PricingRule { IsActive = false };

        rule.IsApplicable(5, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsApplicable_WithQuantity_ShouldReturnTrue_WhenNoQuantityConstraints()
    {
        var now = DateTime.UtcNow;
        var rule = new PricingRule
        {
            IsActive = true,
            StartDate = now.AddHours(-1),
            EndDate = now.AddHours(1),
            MinQuantity = null,
            MaxQuantity = null
        };

        rule.IsApplicable(999, now).Should().BeTrue();
    }

    [Fact]
    public void AppliesToQuantity_ShouldReturnTrue_WhenNoMinMaxSet()
    {
        var rule = new PricingRule { IsActive = true, MinQuantity = null, MaxQuantity = null };

        rule.AppliesToQuantity(1).Should().BeTrue();
        rule.AppliesToQuantity(1000).Should().BeTrue();
    }

    [Fact]
    public void AppliesToQuantity_ShouldReturnTrue_WhenOnlyMinSetAndQuantityAbove()
    {
        var rule = new PricingRule { IsActive = true, MinQuantity = 5, MaxQuantity = null };

        rule.AppliesToQuantity(5).Should().BeTrue();
        rule.AppliesToQuantity(100).Should().BeTrue();
        rule.AppliesToQuantity(4).Should().BeFalse();
    }

    [Fact]
    public void AppliesToQuantity_ShouldReturnTrue_WhenOnlyMaxSetAndQuantityBelow()
    {
        var rule = new PricingRule { IsActive = true, MinQuantity = null, MaxQuantity = 10 };

        rule.AppliesToQuantity(10).Should().BeTrue();
        rule.AppliesToQuantity(1).Should().BeTrue();
        rule.AppliesToQuantity(11).Should().BeFalse();
    }

    [Fact]
    public void CalculatePrice_ShouldReturnBasePrice_WhenQuantityOutOfRange()
    {
        var rule = new PricingRule
        {
            IsActive = true,
            RuleType = PricingRuleType.VolumeDiscount,
            DiscountPercentage = 50,
            MinQuantity = 10,
            MaxQuantity = 20
        };

        rule.CalculatePrice(100m, 5).Should().Be(100m);
    }

    [Fact]
    public void CalculatePrice_ShouldHandleNullDiscountPercentage_ForVolumeDiscount()
    {
        var rule = new PricingRule
        {
            IsActive = true,
            RuleType = PricingRuleType.VolumeDiscount,
            DiscountPercentage = null
        };

        rule.CalculatePrice(100m, 1).Should().Be(100m);
    }

    [Fact]
    public void CalculatePrice_ShouldHandleNullFixedPrice_ForFixedPriceOverride()
    {
        var rule = new PricingRule
        {
            IsActive = true,
            RuleType = PricingRuleType.FixedPriceOverride,
            FixedPrice = null
        };

        rule.CalculatePrice(100m, 1).Should().Be(100m);
    }

    [Fact]
    public void CalculatePrice_ShouldApplyRegionBased_DiscountPercentage_WhenNoFixedPrice()
    {
        var rule = new PricingRule
        {
            IsActive = true,
            RuleType = PricingRuleType.RegionBased,
            FixedPrice = null,
            DiscountPercentage = 10
        };

        rule.CalculatePrice(200m, 1).Should().Be(180m);
    }

    [Fact]
    public void CalculateDiscount_ShouldReturnDiscountAmount_ForBundle()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.Bundle,
            DiscountAmount = 15m
        };

        rule.CalculateDiscount(100m, 2).Should().Be(15m);
    }

    [Fact]
    public void CalculateDiscount_ShouldReturnZero_ForBundleWithNullAmount()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.Bundle,
            DiscountAmount = null
        };

        rule.CalculateDiscount(100m, 2).Should().Be(0m);
    }

    [Fact]
    public void CalculateDiscount_ShouldReturnZero_ForUnknownRuleType()
    {
        var rule = new PricingRule
        {
            RuleType = (PricingRuleType)999
        };

        rule.CalculateDiscount(100m, 2).Should().Be(0m);
    }

    [Fact]
    public void CalculateDiscount_Percentage_ShouldCalculateCorrectly_ForMultipleQuantity()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.Percentage,
            DiscountPercentage = 25
        };

        // 50 * 3 * 25 / 100 = 37.5
        rule.CalculateDiscount(50m, 3).Should().Be(37.5m);
    }

    [Fact]
    public void CalculateDiscount_Percentage_ShouldReturnZero_WhenPercentageIsNull()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.Percentage,
            DiscountPercentage = null
        };

        rule.CalculateDiscount(50m, 3).Should().Be(0m);
    }

    [Fact]
    public void CalculateDiscount_FixedAmount_ShouldReturnZero_WhenAmountIsNull()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.FixedAmount,
            DiscountAmount = null
        };

        rule.CalculateDiscount(100m, 2).Should().Be(0m);
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_ShouldReturnZero_WhenBuyQuantityOnlySet()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.BuyXGetY,
            BuyQuantity = 2,
            GetQuantity = null
        };

        rule.CalculateDiscount(10m, 6).Should().Be(0m);
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_ShouldReturnZero_WhenGetQuantityOnlySet()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.BuyXGetY,
            BuyQuantity = null,
            GetQuantity = 1
        };

        rule.CalculateDiscount(10m, 6).Should().Be(0m);
    }

    [Fact]
    public void CalculateDiscount_VolumeDiscount_WithPriceTier_ShouldCalculateCorrectly()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.VolumeDiscount,
            PricingTiers = { new PricingRuleTier { MinQuantity = 1, MaxQuantity = 100, Price = 8m } }
        };

        // (10 - 8) * 5 = 10
        rule.CalculateDiscount(10m, 5).Should().Be(10m);
    }

    [Fact]
    public void CalculateDiscount_VolumeDiscount_WithPercentageTier_ShouldCalculateCorrectly()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.VolumeDiscount,
            PricingTiers = { new PricingRuleTier { MinQuantity = 1, MaxQuantity = 50, DiscountPercentage = 15 } }
        };

        // 10 * 5 * 15 / 100 = 7.5
        rule.CalculateDiscount(10m, 5).Should().Be(7.5m);
    }

    [Fact]
    public void CalculateDiscount_VolumeDiscount_ShouldSelectHighestDiscountTier()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.VolumeDiscount,
            PricingTiers =
            {
                new PricingRuleTier { MinQuantity = 1, MaxQuantity = 50, DiscountPercentage = 10 },
                new PricingRuleTier { MinQuantity = 1, MaxQuantity = 50, DiscountPercentage = 20 }
            }
        };

        // Should pick 20% discount: 10 * 5 * 20 / 100 = 10
        rule.CalculateDiscount(10m, 5).Should().Be(10m);
    }

    [Fact]
    public void CalculateDiscount_VolumeDiscount_ShouldReturnZero_WhenTierHasNoPriceOrPercentage()
    {
        var rule = new PricingRule
        {
            RuleType = PricingRuleType.VolumeDiscount,
            PricingTiers =
            {
                new PricingRuleTier { MinQuantity = 1, MaxQuantity = 50, Price = null, DiscountPercentage = null }
            }
        };

        rule.CalculateDiscount(10m, 5).Should().Be(0m);
    }

    [Fact]
    public void Properties_ShouldHoldAssignedValues()
    {
        var productId = Guid.NewGuid();
        var rule = new PricingRule
        {
            ProductId = productId,
            Name = "Test Rule",
            Description = "Description",
            RuleType = PricingRuleType.TimeBased,
            Priority = 5,
            IsActive = true,
            TimeStart = "09:00",
            TimeEnd = "17:00",
            DaysOfWeek = "1,2,3,4,5",
            Region = "US",
            CustomerSegment = "Premium"
        };

        rule.ProductId.Should().Be(productId);
        rule.Name.Should().Be("Test Rule");
        rule.Description.Should().Be("Description");
        rule.RuleType.Should().Be(PricingRuleType.TimeBased);
        rule.Priority.Should().Be(5);
        rule.IsActive.Should().BeTrue();
        rule.TimeStart.Should().Be("09:00");
        rule.TimeEnd.Should().Be("17:00");
        rule.DaysOfWeek.Should().Be("1,2,3,4,5");
        rule.Region.Should().Be("US");
        rule.CustomerSegment.Should().Be("Premium");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var rule = new PricingRule();

        rule.Name.Should().BeEmpty();
        rule.IsActive.Should().BeTrue();
        rule.Priority.Should().Be(0);
        rule.ProductId.Should().BeNull();
        rule.Description.Should().BeNull();
        rule.StartDate.Should().BeNull();
        rule.EndDate.Should().BeNull();
        rule.MinQuantity.Should().BeNull();
        rule.MaxQuantity.Should().BeNull();
        rule.DiscountPercentage.Should().BeNull();
        rule.DiscountAmount.Should().BeNull();
        rule.FixedPrice.Should().BeNull();
        rule.BuyQuantity.Should().BeNull();
        rule.GetQuantity.Should().BeNull();
    }
}
