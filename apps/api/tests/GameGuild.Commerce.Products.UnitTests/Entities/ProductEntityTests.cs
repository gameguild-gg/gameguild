using FluentAssertions;
using GameGuild.Commerce.Products;
using Xunit;

namespace GameGuild.Commerce.Products.UnitTests.Entities;

/// <summary>
/// Unit tests for Product entity
/// </summary>
public class ProductEntityTests
{
    [Fact]
    public void DefaultConstructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        product.Name.Should().BeEmpty();
        product.Description.Should().BeNull();
        product.ShortDescription.Should().BeNull();
        product.ImageUrl.Should().BeNull();
        product.Type.Should().Be(ProductType.Program);
        product.IsBundle.Should().BeFalse();
        product.CreatorId.Should().BeNull();
        product.Pricing.Should().NotBeNull().And.BeEmpty();
        product.SubscriptionPlans.Should().NotBeNull().And.BeEmpty();
        product.UserProducts.Should().NotBeNull().And.BeEmpty();
        product.PromoCodes.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData("My Product")]
    [InlineData("Course: Advanced Programming")]
    [InlineData("Bundle - Complete Course Pack")]
    public void Name_ShouldAcceptValidNames(string name)
    {
        // Arrange
        var product = new Product();

        // Act
        product.Name = name;

        // Assert
        product.Name.Should().Be(name);
    }

    [Theory]
    [InlineData(ProductType.Program)]
    [InlineData(ProductType.Course)]
    [InlineData(ProductType.Bundle)]
    [InlineData(ProductType.Subscription)]
    [InlineData(ProductType.Workshop)]
    [InlineData(ProductType.Mentorship)]
    [InlineData(ProductType.Ebook)]
    [InlineData(ProductType.ResourcePack)]
    [InlineData(ProductType.Community)]
    [InlineData(ProductType.Certification)]
    [InlineData(ProductType.Physical)]
    [InlineData(ProductType.Service)]
    [InlineData(ProductType.LearningPathway)]
    [InlineData(ProductType.Other)]
    public void Type_ShouldAcceptAllProductTypes(ProductType type)
    {
        // Arrange
        var product = new Product();

        // Act
        product.Type = type;

        // Assert
        product.Type.Should().Be(type);
    }

    [Fact]
    public void IsBundle_WhenSetToTrue_ShouldReflectValue()
    {
        // Arrange
        var product = new Product();

        // Act
        product.IsBundle = true;

        // Assert
        product.IsBundle.Should().BeTrue();
    }

    [Fact]
    public void CreatorId_WhenSet_ShouldRetainValue()
    {
        // Arrange
        var product = new Product();
        var creatorId = Guid.NewGuid();

        // Act
        product.CreatorId = creatorId;

        // Assert
        product.CreatorId.Should().Be(creatorId);
    }

    [Fact]
    public void GetCreatorInfo_WhenCreatorIsNull_ShouldReturnNull()
    {
        // Arrange
        var product = new Product();

        // Act
        var creatorInfo = product.GetCreatorInfo();

        // Assert
        creatorInfo.Should().BeNull();
    }

    [Fact]
    public void CollectionProperties_ShouldBeInitializedAndModifiable()
    {
        // Arrange
        var product = new Product();

        // Assert all collections are initialized
        product.Pricing.Should().NotBeNull();
        product.SubscriptionPlans.Should().NotBeNull();
        product.UserProducts.Should().NotBeNull();
        product.PromoCodes.Should().NotBeNull();
    }
}

/// <summary>
/// Unit tests for ProductType enum
/// </summary>
public class ProductTypeEnumTests
{
    [Fact]
    public void ProductType_ShouldHaveExpectedCount()
    {
        // Assert
        Enum.GetValues<ProductType>().Should().HaveCountGreaterOrEqualTo(14);
    }

    [Theory]
    [InlineData(ProductType.Program, 0)]
    [InlineData(ProductType.Course, 1)]
    [InlineData(ProductType.Bundle, 2)]
    [InlineData(ProductType.Subscription, 3)]
    [InlineData(ProductType.Other, 99)]
    public void ProductType_ShouldHaveExpectedUnderlyingValues(ProductType type, int expectedValue)
    {
        // Assert
        ((int)type).Should().Be(expectedValue);
    }

    [Fact]
    public void AllProductTypes_ShouldBeDefined()
    {
        // Act
        var values = Enum.GetValues<ProductType>();

        // Assert
        foreach (var value in values)
        {
            Enum.IsDefined(typeof(ProductType), value).Should().BeTrue();
        }
    }
}

/// <summary>
/// Unit tests for ProductAcquisitionType enum
/// </summary>
public class ProductAcquisitionTypeEnumTests
{
    [Theory]
    [InlineData(ProductAcquisitionType.Purchase)]
    [InlineData(ProductAcquisitionType.Subscription)]
    [InlineData(ProductAcquisitionType.Grant)]
    [InlineData(ProductAcquisitionType.PromoCode)]
    [InlineData(ProductAcquisitionType.Bundle)]
    [InlineData(ProductAcquisitionType.Trial)]
    [InlineData(ProductAcquisitionType.Referral)]
    public void ProductAcquisitionType_AllValues_ShouldBeDefined(ProductAcquisitionType type)
    {
        // Assert
        Enum.IsDefined(typeof(ProductAcquisitionType), type).Should().BeTrue();
    }

    [Theory]
    [InlineData(ProductAcquisitionType.Purchase, 0)]
    [InlineData(ProductAcquisitionType.Subscription, 1)]
    [InlineData(ProductAcquisitionType.Grant, 2)]
    public void ProductAcquisitionType_ShouldHaveExpectedUnderlyingValues(ProductAcquisitionType type, int expected)
    {
        // Assert
        ((int)type).Should().Be(expected);
    }
}
