using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.UnitTests.Entities;

public class PricingRuleTierTests
{
    [Fact]
    public void Tier_Should_Hold_Assigned_Values()
    {
        var ruleId = Guid.NewGuid();
        var tier = new PricingRuleTier
        {
            PricingRuleId = ruleId,
            MinQuantity = 1,
            MaxQuantity = 10,
            Price = 9.5m,
            DiscountPercentage = 5m
        };

        tier.PricingRuleId.Should().Be(ruleId);
        tier.MinQuantity.Should().Be(1);
        tier.MaxQuantity.Should().Be(10);
        tier.Price.Should().Be(9.5m);
        tier.DiscountPercentage.Should().Be(5m);
    }

    [Fact]
    public void Constructor_Should_Map_Partial_Values()
    {
        var rule = new PricingRule { Name = "Rule", RuleType = PricingRuleType.FixedAmount };
        var ruleId = Guid.NewGuid();
        var tier = new PricingRuleTier(new
        {
            PricingRuleId = ruleId,
            MinQuantity = 2,
            MaxQuantity = 8,
            Price = 15m,
            DiscountPercentage = 12m,
            PricingRule = rule
        });

        tier.PricingRuleId.Should().Be(ruleId);
        tier.MinQuantity.Should().Be(2);
        tier.MaxQuantity.Should().Be(8);
        tier.Price.Should().Be(15m);
        tier.DiscountPercentage.Should().Be(12m);
        tier.PricingRule.Should().BeSameAs(rule);
    }
}
