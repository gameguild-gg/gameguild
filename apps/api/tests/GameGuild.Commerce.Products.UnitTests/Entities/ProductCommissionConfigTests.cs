using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Products.UnitTests.Entities;

public class ProductCommissionConfigTests
{
    [Fact]
    public void Create_ShouldSetDefaults()
    {
        var productId = Guid.NewGuid();
        var config = ProductCommissionConfig.Create(productId);

        config.ProductId.Should().Be(productId);
        config.IsActive.Should().BeTrue();
        config.ReferralCommissionPercentage.Should().Be(30m);
        config.AffiliateCommissionPercentage.Should().Be(30m);
        config.MaxAffiliateDiscount.Should().Be(0m);
    }

    [Fact]
    public void Create_ShouldThrow_WhenProductIdEmpty()
    {
        var act = () => ProductCommissionConfig.Create(Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_ShouldThrow_WhenReferralPercentageOutOfRange(decimal pct)
    {
        var act = () => ProductCommissionConfig.Create(Guid.NewGuid(), referralCommissionPercentage: pct);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateDefault_ShouldUseDefaultValues()
    {
        var config = ProductCommissionConfig.CreateDefault(Guid.NewGuid());
        config.ReferralCommissionPercentage.Should().Be(30m);
        config.AffiliateCommissionPercentage.Should().Be(30m);
    }

    [Fact]
    public void SetReferralCommission_ShouldUpdateValue()
    {
        var config = ProductCommissionConfig.Create(Guid.NewGuid());
        config.SetReferralCommission(50m);
        config.ReferralCommissionPercentage.Should().Be(50m);
    }

    [Fact]
    public void SetReferralCommission_ShouldThrow_WhenOutOfRange()
    {
        var config = ProductCommissionConfig.Create(Guid.NewGuid());
        var act = () => config.SetReferralCommission(101m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetAffiliateCommission_ShouldUpdateValue()
    {
        var config = ProductCommissionConfig.Create(Guid.NewGuid());
        config.SetAffiliateCommission(10m);
        config.AffiliateCommissionPercentage.Should().Be(10m);
    }

    [Fact]
    public void SetMaxAffiliateDiscount_ShouldUpdateValue()
    {
        var config = ProductCommissionConfig.Create(Guid.NewGuid());
        config.SetMaxAffiliateDiscount(75m);
        config.MaxAffiliateDiscount.Should().Be(75m);
    }

    [Fact]
    public void ConfigureRecurringCommissions_ShouldEnable()
    {
        var config = ProductCommissionConfig.Create(Guid.NewGuid());
        config.ConfigureRecurringCommissions(true, 12);
        config.CommissionOnRecurring.Should().BeTrue();
        config.MaxRecurringPayments.Should().Be(12);
    }

    [Fact]
    public void ConfigureRecurringCommissions_ShouldDisable_AndClearMax()
    {
        var config = ProductCommissionConfig.Create(Guid.NewGuid());
        config.ConfigureRecurringCommissions(true, 5);
        config.ConfigureRecurringCommissions(false);
        config.CommissionOnRecurring.Should().BeFalse();
        config.MaxRecurringPayments.Should().BeNull();
    }

    [Fact]
    public void ConfigureRecurringCommissions_ShouldThrow_WhenMaxLessThanOne()
    {
        var config = ProductCommissionConfig.Create(Guid.NewGuid());
        var act = () => config.ConfigureRecurringCommissions(true, 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetActive_ShouldToggle()
    {
        var config = ProductCommissionConfig.Create(Guid.NewGuid());
        config.SetActive(false);
        config.IsActive.Should().BeFalse();
    }

    [Fact]
    public void CalculateReferralCommission_ShouldCalculateCorrectly()
    {
        var config = ProductCommissionConfig.Create(Guid.NewGuid(), referralCommissionPercentage: 10m);
        config.CalculateReferralCommission(100m).Should().Be(10m);
    }

    [Fact]
    public void CalculateReferralCommission_ShouldReturnZero_WhenInactive()
    {
        var config = ProductCommissionConfig.Create(Guid.NewGuid());
        config.SetActive(false);
        config.CalculateReferralCommission(100m).Should().Be(0m);
    }

    [Fact]
    public void CalculateAffiliateCommission_ShouldCalculateCorrectly()
    {
        var config = ProductCommissionConfig.Create(Guid.NewGuid(), affiliateCommissionPercentage: 20m);
        config.CalculateAffiliateCommission(200m).Should().Be(40m);
    }

    [Fact]
    public void CalculateAffiliateCommission_ShouldReturnZero_WhenInactive()
    {
        var config = ProductCommissionConfig.Create(Guid.NewGuid());
        config.SetActive(false);
        config.CalculateAffiliateCommission(100m).Should().Be(0m);
    }
}
