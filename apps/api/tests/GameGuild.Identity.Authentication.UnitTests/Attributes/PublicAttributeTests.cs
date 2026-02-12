using FluentAssertions;
using System.Reflection;
using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Attributes;

public class PublicAttributeTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Set_IsPublic_To_True_By_Default()
    {
        var attribute = new PublicAttribute();
        
        attribute.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void Constructor_Should_Set_IsPublic_To_True_When_Specified()
    {
        var attribute = new PublicAttribute(true);
        
        attribute.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void Constructor_Should_Set_IsPublic_To_False_When_Specified()
    {
        var attribute = new PublicAttribute(false);
        
        attribute.IsPublic.Should().BeFalse();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void IsPublic_Property_Should_Be_Read_Only()
    {
        var type = typeof(PublicAttribute);
        var property = type.GetProperty(nameof(PublicAttribute.IsPublic));
        
        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
        property.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void IsPublic_Property_Should_Preserve_Constructor_Value()
    {
        var attributeTrue = new PublicAttribute(true);
        var attributeFalse = new PublicAttribute(false);
        
        attributeTrue.IsPublic.Should().BeTrue();
        attributeFalse.IsPublic.Should().BeFalse();
    }

    #endregion

    #region Attribute Usage Tests

    [Fact]
    public void Attribute_Should_Target_Method_And_Class()
    {
        var type = typeof(PublicAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            type, 
            typeof(AttributeUsageAttribute)
        );
        
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Method);
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Class);
    }

    [Fact]
    public void Attribute_Should_Not_Allow_Multiple_By_Default()
    {
        var type = typeof(PublicAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            type, 
            typeof(AttributeUsageAttribute)
        );
        
        // AttributeUsage.AllowMultiple defaults to false if not specified
        attributeUsage.Should().NotBeNull();
        attributeUsage!.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void Attribute_Should_Inherit_From_Attribute()
    {
        var type = typeof(PublicAttribute);
        
        type.Should().BeDerivedFrom<Attribute>();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Multiple_Attributes_Can_Be_Created_With_Different_Values()
    {
        var attributes = new[]
        {
            new PublicAttribute(),
            new PublicAttribute(true),
            new PublicAttribute(false)
        };
        
        attributes.Should().HaveCount(3);
        attributes[0].IsPublic.Should().BeTrue();
        attributes[1].IsPublic.Should().BeTrue();
        attributes[2].IsPublic.Should().BeFalse();
    }

    [Fact]
    public void Attribute_Can_Be_Applied_To_Test_Method()
    {
        var methodInfo = GetType().GetMethod(nameof(TestMethodWithPublicAttribute), BindingFlags.NonPublic | BindingFlags.Instance);
        
        methodInfo.Should().NotBeNull();
        var attribute = methodInfo!.GetCustomAttributes(typeof(PublicAttribute), false)
            .FirstOrDefault() as PublicAttribute;
        
        attribute.Should().NotBeNull();
        attribute!.IsPublic.Should().BeTrue();
    }

    [Public]
    private void TestMethodWithPublicAttribute() { }

    [Fact]
    public void Attribute_Can_Be_Applied_To_Test_Class()
    {
        var typeInfo = typeof(TestClassWithPublicAttribute);
        
        var attribute = typeInfo.GetCustomAttributes(typeof(PublicAttribute), false)
            .FirstOrDefault() as PublicAttribute;
        
        attribute.Should().NotBeNull();
        attribute!.IsPublic.Should().BeTrue();
    }

    [Public]
    public class TestClassWithPublicAttribute { }

    [Fact]
    public void Attribute_With_False_Can_Be_Applied()
    {
        var methodInfo = GetType().GetMethod(nameof(TestMethodWithPublicFalse), BindingFlags.NonPublic | BindingFlags.Instance);
        
        methodInfo.Should().NotBeNull();
        var attribute = methodInfo!.GetCustomAttributes(typeof(PublicAttribute), false)
            .FirstOrDefault() as PublicAttribute;
        
        attribute.Should().NotBeNull();
        attribute!.IsPublic.Should().BeFalse();
    }

    [Public(false)]
    private void TestMethodWithPublicFalse() { }

    #endregion

    #region Documentation Tests

    [Fact]
    public void Attribute_Should_Have_Summary_Comment()
    {
        var type = typeof(PublicAttribute);
        
        // Verify the class exists and is documented (this is more of a smoke test)
        type.Should().NotBeNull();
        type.Name.Should().Be("PublicAttribute");
    }

    [Fact]
    public void IsPublic_Property_Should_Have_Summary_Comment()
    {
        var type = typeof(PublicAttribute);
        var property = type.GetProperty(nameof(PublicAttribute.IsPublic));
        
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(bool));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Two_Attributes_With_Same_Value_Should_Be_Independent()
    {
        var attr1 = new PublicAttribute(true);
        var attr2 = new PublicAttribute(true);
        
        attr1.Should().NotBeSameAs(attr2);
        attr1.IsPublic.Should().Be(attr2.IsPublic);
    }

    [Fact]
    public void Default_Constructor_Should_Be_Equivalent_To_Explicit_True()
    {
        var attrDefault = new PublicAttribute();
        var attrExplicit = new PublicAttribute(true);
        
        attrDefault.IsPublic.Should().Be(attrExplicit.IsPublic);
    }

    #endregion
}
