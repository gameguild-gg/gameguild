using FluentAssertions;
using GameGuild.Commerce.Products;
using Xunit;

namespace GameGuild.Commerce.Products.UnitTests.Entities;

/// <summary>
/// Unit tests for ProductPricing entity
/// </summary>
public class ProductPricingEntityTests
{
    #region Constructor Tests

    [Fact]
    public void DefaultConstructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var pricing = new ProductPricing();

        // Assert
        pricing.Name.Should().BeEmpty();
        pricing.Currency.Should().Be("USD");
        pricing.IsDefault.Should().BeFalse();
        pricing.CurrentVersion.Should().Be(1);
        pricing.Versions.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #region GetCurrentPrice Tests

    [Fact]
    public void GetCurrentPrice_WhenNoSale_ShouldReturnBasePrice()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m,
            salePrice: null);

        // Act
        var result = pricing.GetCurrentPrice();

        // Assert
        result.Should().Be(99.99m);
    }

    [Fact]
    public void GetCurrentPrice_WhenSaleActive_ShouldReturnSalePrice()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m,
            currency: "USD",
            salePrice: 79.99m,
            saleStartDate: DateTime.UtcNow.AddDays(-1),
            saleEndDate: DateTime.UtcNow.AddDays(1),
            isDefault: false);

        // Act
        var result = pricing.GetCurrentPrice();

        // Assert
        result.Should().Be(79.99m);
    }

    [Fact]
    public void GetCurrentPrice_WhenSaleExpired_ShouldReturnBasePrice()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m,
            currency: "USD",
            salePrice: 79.99m,
            saleStartDate: DateTime.UtcNow.AddDays(-10),
            saleEndDate: DateTime.UtcNow.AddDays(-1),
            isDefault: false);

        // Act
        var result = pricing.GetCurrentPrice();

        // Assert
        result.Should().Be(99.99m);
    }

    [Fact]
    public void GetCurrentPrice_WhenSaleNotStarted_ShouldReturnBasePrice()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m,
            currency: "USD",
            salePrice: 79.99m,
            saleStartDate: DateTime.UtcNow.AddDays(1),
            saleEndDate: DateTime.UtcNow.AddDays(10),
            isDefault: false);

        // Act
        var result = pricing.GetCurrentPrice();

        // Assert
        result.Should().Be(99.99m);
    }

    #endregion

    #region IsSaleActive Tests

    [Fact]
    public void IsSaleActive_WhenNoSalePrice_ShouldReturnFalse()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m);

        // Act
        var result = pricing.IsSaleActive();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSaleActive_WhenWithinSalePeriod_ShouldReturnTrue()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m,
            currency: "USD",
            salePrice: 79.99m,
            saleStartDate: DateTime.UtcNow.AddDays(-1),
            saleEndDate: DateTime.UtcNow.AddDays(1),
            isDefault: false);

        // Act
        var result = pricing.IsSaleActive();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSaleActive_WhenSaleExpired_ShouldReturnFalse()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m,
            currency: "USD",
            salePrice: 79.99m,
            saleStartDate: DateTime.UtcNow.AddDays(-10),
            saleEndDate: DateTime.UtcNow.AddDays(-1),
            isDefault: false);

        // Act
        var result = pricing.IsSaleActive();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSaleActive_WhenNoDates_ShouldReturnTrue()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m,
            salePrice: 79.99m);

        // Act
        var result = pricing.IsSaleActive();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region UpdateBasePrice Tests

    [Fact]
    public void UpdateBasePrice_WithValidPrice_ShouldUpdateAndIncrementVersion()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m);
        var initialVersion = pricing.CurrentVersion;

        // Act
        var version = pricing.UpdateBasePrice(119.99m, "Price increase");

        // Assert
        pricing.GetCurrentPrice().Should().Be(119.99m);
        pricing.CurrentVersion.Should().Be(initialVersion + 1);
        version.Should().NotBeNull();
        version.BasePrice.Should().Be(119.99m);
        version.ChangeReason.Should().Be("Price increase");
    }

    [Fact]
    public void UpdateBasePrice_WithNegativePrice_ShouldThrow()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m);

        // Act
        var act = () => pricing.UpdateBasePrice(-10m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be negative*");
    }

    [Fact]
    public void UpdateBasePrice_WithZeroPrice_ShouldSucceed()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m);

        // Act
        var version = pricing.UpdateBasePrice(0m, "Free product");

        // Assert
        pricing.GetCurrentPrice().Should().Be(0m);
        version.BasePrice.Should().Be(0m);
    }

    #endregion

    #region UpdateSalePrice Tests

    [Fact]
    public void UpdateSalePrice_WithValidPrice_ShouldUpdateAndIncrementVersion()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m);
        var initialVersion = pricing.CurrentVersion;

        // Act
        var version = pricing.UpdateSalePrice(79.99m, "Holiday sale");

        // Assert
        pricing.CurrentVersion.Should().Be(initialVersion + 1);
        version.Should().NotBeNull();
        version.SalePrice.Should().Be(79.99m);
        version.ChangeReason.Should().Be("Holiday sale");
    }

    [Fact]
    public void UpdateSalePrice_WithNull_ShouldClearSalePrice()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m,
            salePrice: 79.99m);

        // Act
        var version = pricing.UpdateSalePrice(null, "Sale ended");

        // Assert
        version.SalePrice.Should().BeNull();
        pricing.IsSaleActive().Should().BeFalse();
    }

    [Fact]
    public void UpdateSalePrice_WithNegativePrice_ShouldThrow()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m);

        // Act
        var act = () => pricing.UpdateSalePrice(-10m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be negative*");
    }

    #endregion

    #region UpdatePrices Tests

    [Fact]
    public void UpdatePrices_WithValidPrices_ShouldUpdateBoth()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m);
        var initialVersion = pricing.CurrentVersion;

        // Act
        var version = pricing.UpdatePrices(149.99m, 119.99m, "Price adjustment");

        // Assert
        pricing.CurrentVersion.Should().Be(initialVersion + 1);
        version.BasePrice.Should().Be(149.99m);
        version.SalePrice.Should().Be(119.99m);
    }

    [Fact]
    public void UpdatePrices_WhenSalePriceGreaterThanBase_ShouldThrow()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m);

        // Act
        var act = () => pricing.UpdatePrices(99.99m, 119.99m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be less than base price*");
    }

    [Fact]
    public void UpdatePrices_WhenSalePriceEqualsBase_ShouldThrow()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m);

        // Act
        var act = () => pricing.UpdatePrices(99.99m, 99.99m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be less than base price*");
    }

    [Fact]
    public void UpdatePrices_WithNegativeBasePrice_ShouldThrow()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m);

        // Act
        var act = () => pricing.UpdatePrices(-10m, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be negative*");
    }

    [Fact]
    public void UpdatePrices_WithNegativeSalePrice_ShouldThrow()
    {
        // Arrange
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: 99.99m);

        // Act
        var act = () => pricing.UpdatePrices(99.99m, -10m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be negative*");
    }

    #endregion

    #region CreateWithVersion Factory Tests

    [Fact]
    public void CreateWithVersion_WithValidData_ShouldCreatePricingAndVersion()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var (pricing, version) = ProductPricing.CreateWithVersion(
            productId: productId,
            name: "Standard",
            basePrice: 99.99m,
            currency: "EUR",
            salePrice: 79.99m,
            saleStartDate: DateTime.UtcNow,
            saleEndDate: DateTime.UtcNow.AddDays(7),
            isDefault: true);

        // Assert
        pricing.ProductId.Should().Be(productId);
        pricing.Name.Should().Be("Standard");
        pricing.Currency.Should().Be("EUR");
        pricing.IsDefault.Should().BeTrue();
        pricing.CurrentVersion.Should().Be(1);
        version.Should().NotBeNull();
        version.PriceVersion.Should().Be(1);
        version.BasePrice.Should().Be(99.99m);
        version.SalePrice.Should().Be(79.99m);
    }

    [Fact]
    public void CreateWithVersion_WithEmptyProductId_ShouldThrow()
    {
        // Act
        var act = () => ProductPricing.CreateWithVersion(
            productId: Guid.Empty,
            name: "Standard",
            basePrice: 99.99m,
            currency: "USD",
            salePrice: null,
            saleStartDate: null,
            saleEndDate: null,
            isDefault: false);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Product ID is required*");
    }

    [Fact]
    public void CreateWithVersion_WithNegativeBasePrice_ShouldThrow()
    {
        // Act
        var act = () => ProductPricing.CreateWithVersion(
            productId: Guid.NewGuid(),
            name: "Standard",
            basePrice: -10m,
            currency: "USD",
            salePrice: null,
            saleStartDate: null,
            saleEndDate: null,
            isDefault: false);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be negative*");
    }

    [Fact]
    public void CreateWithVersion_MinimalParameters_ShouldUseDefaults()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var (pricing, version) = ProductPricing.CreateWithVersion(
            productId: productId,
            name: "Standard",
            basePrice: 49.99m);

        // Assert
        pricing.Currency.Should().Be("USD");
        pricing.IsDefault.Should().BeFalse();
        version.SalePrice.Should().BeNull();
    }

    #endregion
}
