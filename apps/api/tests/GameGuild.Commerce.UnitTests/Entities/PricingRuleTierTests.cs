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
}
