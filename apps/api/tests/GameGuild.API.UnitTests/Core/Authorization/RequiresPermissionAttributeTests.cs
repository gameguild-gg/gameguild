using FluentAssertions;
using GameGuild.API.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Xunit;

namespace GameGuild.Tests.API.Unit.Authorization;

public class RequiresPermissionAttributeTests
{
    [Fact]
    public void Constructor_Should_Set_Permission_Name()
    {
        var permissionName = "users.read";
        
        var attribute = new RequiresPermissionAttribute(permissionName);
        
        attribute.Name.Should().Be(permissionName);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Name_Is_Null()
    {
        var act = () => new RequiresPermissionAttribute(null!);
        
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name")
            .WithMessage("Permission name cannot be null or whitespace.*");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Name_Is_Empty()
    {
        var act = () => new RequiresPermissionAttribute("");
        
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name")
            .WithMessage("Permission name cannot be null or whitespace.*");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Name_Is_Whitespace()
    {
        var act = () => new RequiresPermissionAttribute("   ");
        
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name")
            .WithMessage("Permission name cannot be null or whitespace.*");
    }

    [Fact]
    public void Attribute_Should_Implement_IFilterMetadata()
    {
        var attribute = new RequiresPermissionAttribute("test.permission");
        
        attribute.Should().BeAssignableTo<IFilterMetadata>();
    }

    [Fact]
    public void Attribute_Should_Be_Sealed()
    {
        var type = typeof(RequiresPermissionAttribute);
        
        type.IsSealed.Should().BeTrue();
    }

    [Fact]
    public void Attribute_Should_Allow_Multiple_Usage()
    {
        var type = typeof(RequiresPermissionAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            type, 
            typeof(AttributeUsageAttribute)
        );
        
        attributeUsage.Should().NotBeNull();
        attributeUsage!.AllowMultiple.Should().BeTrue();
    }

    [Fact]
    public void Attribute_Should_Target_Class_And_Method()
    {
        var type = typeof(RequiresPermissionAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            type, 
            typeof(AttributeUsageAttribute)
        );
        
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Class);
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Method);
    }

    [Theory]
    [InlineData("users.read")]
    [InlineData("users.write")]
    [InlineData("admin.dashboard")]
    [InlineData("projects.create")]
    [InlineData("billing.manage")]
    public void Constructor_Should_Handle_Various_Permission_Names(string permissionName)
    {
        var attribute = new RequiresPermissionAttribute(permissionName);
        
        attribute.Name.Should().Be(permissionName);
    }

    [Fact]
    public void Name_Property_Should_Be_Read_Only()
    {
        var type = typeof(RequiresPermissionAttribute);
        var property = type.GetProperty(nameof(RequiresPermissionAttribute.Name));
        
        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
        property.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void Multiple_Attributes_Can_Be_Applied()
    {
        // This test verifies that multiple attributes can be instantiated
        var attr1 = new RequiresPermissionAttribute("users.read");
        var attr2 = new RequiresPermissionAttribute("users.write");
        var attr3 = new RequiresPermissionAttribute("users.delete");
        
        var attributes = new[] { attr1, attr2, attr3 };
        
        attributes.Should().HaveCount(3);
        attributes[0].Name.Should().Be("users.read");
        attributes[1].Name.Should().Be("users.write");
        attributes[2].Name.Should().Be("users.delete");
    }

    [Fact]
    public void Attribute_Should_Preserve_Permission_Name_Exactly()
    {
        var permissionName = "Users.Read.Sensitive.Data";
        
        var attribute = new RequiresPermissionAttribute(permissionName);
        
        attribute.Name.Should().Be(permissionName);
        attribute.Name.Should().NotBe(permissionName.ToLower());
        attribute.Name.Should().NotBe(permissionName.ToUpper());
    }
}
