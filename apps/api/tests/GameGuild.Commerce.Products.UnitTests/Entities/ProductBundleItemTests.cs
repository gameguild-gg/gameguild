using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Products.UnitTests.Entities;

public class ProductBundleItemTests
{
    private readonly Guid _bundleId = Guid.NewGuid();
    private readonly Guid _includedId = Guid.NewGuid();

    [Fact]
    public void Create_ShouldSetProperties()
    {
        var tenantId = Guid.NewGuid();
        var item = ProductBundleItem.Create(_bundleId, _includedId, 3, 2, false, tenantId);

        item.BundleProductId.Should().Be(_bundleId);
        item.IncludedProductId.Should().Be(_includedId);
        item.Quantity.Should().Be(3);
        item.DisplayOrder.Should().Be(2);
        item.IsRequired.Should().BeFalse();
        item.TenantId.Should().Be(tenantId);
        item.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldThrow_WhenBundleProductIdEmpty()
    {
        var act = () => ProductBundleItem.Create(Guid.Empty, _includedId);
        act.Should().Throw<ArgumentException>().WithParameterName("bundleProductId");
    }

    [Fact]
    public void Create_ShouldThrow_WhenIncludedProductIdEmpty()
    {
        var act = () => ProductBundleItem.Create(_bundleId, Guid.Empty);
        act.Should().Throw<ArgumentException>().WithParameterName("includedProductId");
    }

    [Fact]
    public void Create_ShouldThrow_WhenSelfReference()
    {
        var id = Guid.NewGuid();
        var act = () => ProductBundleItem.Create(id, id);
        act.Should().Throw<ArgumentException>().WithParameterName("includedProductId");
    }

    [Fact]
    public void Create_ShouldThrow_WhenQuantityLessThanOne()
    {
        var act = () => ProductBundleItem.Create(_bundleId, _includedId, 0);
        act.Should().Throw<ArgumentException>().WithParameterName("quantity");
    }

    [Fact]
    public void SetQuantity_ShouldUpdateValue()
    {
        var item = ProductBundleItem.Create(_bundleId, _includedId);
        item.SetQuantity(5);
        item.Quantity.Should().Be(5);
    }

    [Fact]
    public void SetQuantity_ShouldThrow_WhenLessThanOne()
    {
        var item = ProductBundleItem.Create(_bundleId, _includedId);
        var act = () => item.SetQuantity(0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetDisplayOrder_ShouldUpdateValue()
    {
        var item = ProductBundleItem.Create(_bundleId, _includedId);
        item.SetDisplayOrder(10);
        item.DisplayOrder.Should().Be(10);
    }

    [Fact]
    public void SetRequired_ShouldUpdateValue()
    {
        var item = ProductBundleItem.Create(_bundleId, _includedId);
        item.SetRequired(false);
        item.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void SetBundleDiscount_ShouldSetValue()
    {
        var item = ProductBundleItem.Create(_bundleId, _includedId);
        item.SetBundleDiscount(25.5m);
        item.BundleDiscountPercentage.Should().Be(25.5m);
    }

    [Fact]
    public void SetBundleDiscount_ShouldAcceptNull()
    {
        var item = ProductBundleItem.Create(_bundleId, _includedId);
        item.SetBundleDiscount(50m);
        item.SetBundleDiscount(null);
        item.BundleDiscountPercentage.Should().BeNull();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void SetBundleDiscount_ShouldThrow_WhenOutOfRange(decimal discount)
    {
        var item = ProductBundleItem.Create(_bundleId, _includedId);
        var act = () => item.SetBundleDiscount(discount);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
