using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using GameGuild.Commerce.Products;
using Xunit;

namespace GameGuild.Commerce.Products.UnitTests;

public class ProductValidationTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCatalogProduct()
    {
        var creatorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var product = Product.Create(
            "Owner Portal Pro",
            ProductType.Subscription,
            "Owner portal with statements and approvals.",
            "Owner portal",
            "https://cdn.example.com/owner-portal.png",
            creatorId,
            isBundle: false,
            tenantId);

        product.Id.Should().NotBeEmpty();
        product.Name.Should().Be("Owner Portal Pro");
        product.Type.Should().Be(ProductType.Subscription);
        product.Description.Should().Be("Owner portal with statements and approvals.");
        product.ShortDescription.Should().Be("Owner portal");
        product.ImageUrl.Should().Be("https://cdn.example.com/owner-portal.png");
        product.CreatorId.Should().Be(creatorId);
        product.TenantId.Should().Be(tenantId);
        product.IsBundle.Should().BeFalse();
        product.IsPublished.Should().BeTrue();
        product.Pricing.Should().BeEmpty();
        product.BundleItems.Should().BeEmpty();
    }

    [Fact]
    public void EmptyName_ShouldFailDataAnnotationsValidation()
    {
        var product = Product.Create("");
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            product,
            new ValidationContext(product),
            results,
            validateAllProperties: true);

        isValid.Should().BeFalse();
        results.Should().Contain(result => result.MemberNames.Contains(nameof(Product.Name)));
    }

    [Fact]
    public void CreateWithVersion_WithNegativeBasePrice_ShouldThrowValidationException()
    {
        var act = () => ProductPricing.CreateWithVersion(
            Guid.NewGuid(),
            "Default",
            -1m,
            isDefault: true);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Base price cannot be negative*");
    }

    [Fact]
    public void AddToBundleTypeSafe_ShouldStoreIncludedProductsInDisplayOrder()
    {
        var bundle = Product.Create("Brokerage Launch Bundle", ProductType.Bundle, isBundle: true);
        var firstIncluded = Guid.NewGuid();
        var secondIncluded = Guid.NewGuid();

        var second = bundle.AddToBundleTypeSafe(secondIncluded, quantity: 2, displayOrder: 20);
        var first = bundle.AddToBundleTypeSafe(firstIncluded, quantity: 1, displayOrder: 10);

        first.BundleProductId.Should().Be(bundle.Id);
        first.IncludedProductId.Should().Be(firstIncluded);
        first.Quantity.Should().Be(1);
        second.Quantity.Should().Be(2);
        bundle.GetBundleProductIds().Should().Equal(firstIncluded, secondIncluded);
    }

    [Fact]
    public void AddToBundleTypeSafe_ShouldRejectNonBundleAndDuplicateItems()
    {
        var product = Product.Create("Standalone Product");
        var includedProductId = Guid.NewGuid();

        product.Invoking(p => p.AddToBundleTypeSafe(includedProductId))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*non-bundle*");

        var bundle = Product.Create("Bundle", ProductType.Bundle, isBundle: true);
        bundle.AddToBundleTypeSafe(includedProductId);

        bundle.Invoking(p => p.AddToBundleTypeSafe(includedProductId))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*already in this bundle*");
    }
}
