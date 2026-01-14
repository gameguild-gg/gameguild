using FluentAssertions;
using Xunit;

namespace GameGuild.Resources.UnitTests.Attributes;

/// <summary>
/// Unit tests for RequiresQuotaAttribute
/// </summary>
public class RequiresQuotaAttributeTests
{
    [Fact]
    public void Constructor_ShouldSetResourceTypeAndAmount()
    {
        // Arrange & Act
        var attribute = new RequiresQuotaAttribute(ResourceUsageType.Users, 5);

        // Assert
        attribute.ResourceType.Should().Be(ResourceUsageType.Users);
        attribute.Amount.Should().Be(5);
    }

    [Fact]
    public void Constructor_WithDefaultAmount_ShouldSet1()
    {
        // Arrange & Act
        var attribute = new RequiresQuotaAttribute(ResourceUsageType.Projects);

        // Assert
        attribute.ResourceType.Should().Be(ResourceUsageType.Projects);
        attribute.Amount.Should().Be(1);
    }

    [Fact]
    public void RecordUsage_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var attribute = new RequiresQuotaAttribute(ResourceUsageType.Storage, 1024);

        // Assert
        attribute.RecordUsage.Should().BeTrue();
    }

    [Fact]
    [Obsolete("Test for deprecated property - can be removed when EnforceHardLimit is removed")]
#pragma warning disable CS0618 // EnforceHardLimit is obsolete
    public void EnforceHardLimit_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var attribute = new RequiresQuotaAttribute(ResourceUsageType.ApiCalls);

        // Assert - hard limits are always enforced now
        attribute.EnforceHardLimit.Should().BeTrue();
    }
#pragma warning restore CS0618

    [Fact]
    public void Source_ShouldBeSettable()
    {
        // Arrange & Act
        var attribute = new RequiresQuotaAttribute(ResourceUsageType.Users)
        {
            Source = "CreateUserCommand"
        };

        // Assert
        attribute.Source.Should().Be("CreateUserCommand");
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        // Arrange & Act
#pragma warning disable CS0618 // EnforceHardLimit is obsolete - test verifies property exists but hard limits are always enforced
        var attribute = new RequiresQuotaAttribute(ResourceUsageType.Storage, 2048)
        {
            Source = "UploadFile",
            RecordUsage = false,
            EnforceHardLimit = false // Deprecated: hard limits are always enforced regardless
        };
#pragma warning restore CS0618

        // Assert
        attribute.ResourceType.Should().Be(ResourceUsageType.Storage);
        attribute.Amount.Should().Be(2048);
        attribute.Source.Should().Be("UploadFile");
        attribute.RecordUsage.Should().BeFalse();
        // Note: EnforceHardLimit=false is ignored - hard limits are always enforced
    }

    [Theory]
    [InlineData(ResourceUsageType.Users)]
    [InlineData(ResourceUsageType.Projects)]
    [InlineData(ResourceUsageType.Storage)]
    [InlineData(ResourceUsageType.ApiCalls)]
    public void Constructor_ShouldAcceptAllResourceTypes(ResourceUsageType resourceType)
    {
        // Arrange & Act
        var attribute = new RequiresQuotaAttribute(resourceType);

        // Assert
        attribute.ResourceType.Should().Be(resourceType);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1024)]
    [InlineData(1048576)]
    public void Constructor_ShouldAcceptVariousAmounts(long amount)
    {
        // Arrange & Act
        var attribute = new RequiresQuotaAttribute(ResourceUsageType.Storage, amount);

        // Assert
        attribute.Amount.Should().Be(amount);
    }

    [Fact]
    public void Attribute_ShouldBeApplicableToClasses()
    {
        // Arrange
        var attributeUsage = typeof(RequiresQuotaAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Class);
    }

    [Fact]
    public void Attribute_ShouldNotAllowMultiple()
    {
        // Arrange
        var attributeUsage = typeof(RequiresQuotaAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.AllowMultiple.Should().BeFalse();
    }
}
