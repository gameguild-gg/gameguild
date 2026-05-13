using FluentAssertions;
using Microsoft.Extensions.Configuration;
using GameGuild.CQRS.Models;

namespace GameGuild.Tests.SharedKernel.Unit.Entities;

public class EntityPropertyMapperTests
{
    [Fact]
    public void ConvertToTargetType_GuidFromString_Converts()
    {
        var guid = Guid.NewGuid();

        var result = EntityPropertyMapper.ConvertToTargetType(guid.ToString(), typeof(Guid));

        result.Should().Be(guid);
    }

    [Fact]
    public void ConvertToTargetType_InvalidGuidString_Throws()
    {
        var act = () => EntityPropertyMapper.ConvertToTargetType("not-a-guid", typeof(Guid));

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void ConvertToTargetType_TenantIdFromString_Converts()
    {
        var guid = Guid.NewGuid();

        var result = EntityPropertyMapper.ConvertToTargetType(guid.ToString(), typeof(TenantId));

        result.Should().BeOfType<TenantId>();
        ((TenantId)result).Value.Should().Be(guid);
    }

    [Fact]
    public void ConvertToTargetType_TenantIdFromGuid_Converts()
    {
        var guid = Guid.NewGuid();

        var result = EntityPropertyMapper.ConvertToTargetType(guid, typeof(TenantId));

        result.Should().BeOfType<TenantId>();
    }

    [Fact]
    public void ConvertToTargetType_TenantIdFromTenantId_ReturnsSame()
    {
        var tenantId = new TenantId(Guid.NewGuid());

        var result = EntityPropertyMapper.ConvertToTargetType(tenantId, typeof(TenantId));

        result.Should().Be(tenantId);
    }

    [Fact]
    public void ConvertToTargetType_InvalidTypeTenantId_Throws()
    {
        var act = () => EntityPropertyMapper.ConvertToTargetType(123, typeof(TenantId));

        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void ConvertToTargetType_NullableTenantIdFromString_Converts()
    {
        var guid = Guid.NewGuid();

        var result = EntityPropertyMapper.ConvertToTargetType(guid.ToString(), typeof(TenantId?));

        result.Should().BeOfType<TenantId>();
    }

    [Fact]
    public void ConvertToTargetType_SameType_ReturnsValue()
    {
        var result = EntityPropertyMapper.ConvertToTargetType("hello", typeof(string));

        result.Should().Be("hello");
    }

    [Fact]
    public void ConvertToTargetType_AssignableType_ReturnsValue()
    {
        var list = new List<string>();

        var result = EntityPropertyMapper.ConvertToTargetType(list, typeof(IEnumerable<string>));

        result.Should().BeSameAs(list);
    }

    [Fact]
    public void ConvertToTargetType_ChangeType_ConvertsIntToLong()
    {
        var result = EntityPropertyMapper.ConvertToTargetType(42, typeof(long));

        result.Should().Be(42L);
    }

    [Fact]
    public void GetProperties_ReturnsReadableProperties()
    {
        var entity = new TestPropertyEntity { Name = "Test", Value = 42 };

        var result = EntityPropertyMapper.GetProperties(entity);

        result.Should().ContainKey("Name");
        result["Name"].Should().Be("Test");
        result.Should().ContainKey("Value");
        result["Value"].Should().Be(42);
    }

    [Fact]
    public void ToDictionary_FromExistingDictionary_ReturnsSameInstance()
    {
        var dictionary = new Dictionary<string, object?> { ["Key"] = "Val" };

        var result = EntityPropertyMapper.ToDictionary(dictionary);

        result.Should().BeSameAs(dictionary);
    }

    [Fact]
    public void ToDictionary_FromAnonymousObject_CreatesDictionary()
    {
        var source = new { Name = "Test", Count = 5 };

        var result = EntityPropertyMapper.ToDictionary(source);

        result.Should().ContainKey("Name");
        result["Name"].Should().Be("Test");
        result.Should().ContainKey("Count");
        result["Count"].Should().Be(5);
    }

    [Fact]
    public void IsNullableProperty_ReferenceType_ReturnsTrue()
    {
        var property = typeof(TestPropertyEntity).GetProperty(nameof(TestPropertyEntity.Name))!;

        EntityPropertyMapper.IsNullableProperty(property).Should().BeTrue();
    }

    [Fact]
    public void IsNullableProperty_ValueType_ReturnsFalse()
    {
        var property = typeof(TestPropertyEntity).GetProperty(nameof(TestPropertyEntity.Value))!;

        EntityPropertyMapper.IsNullableProperty(property).Should().BeFalse();
    }

    [Fact]
    public void IsNullableProperty_NullableValueType_ReturnsTrue()
    {
        var property = typeof(TestPropertyEntity).GetProperty(nameof(TestPropertyEntity.NullableInt))!;

        EntityPropertyMapper.IsNullableProperty(property).Should().BeTrue();
    }

    [Fact]
    public void SetProperties_NullToNullableProperty_SetsNull()
    {
        var target = new SetPropsTarget { NullableName = "before" };
        var properties = new Dictionary<string, object?> { ["NullableName"] = null };

        EntityPropertyMapper.SetProperties(target, properties);

        target.NullableName.Should().BeNull();
    }

    [Fact]
    public void SetProperties_NullToNonNullable_ThrowsInvalidOperation()
    {
        var target = new SetPropsTarget { Count = 5 };
        var properties = new Dictionary<string, object?> { ["Count"] = null };

        var act = () => EntityPropertyMapper.SetProperties(target, properties);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*non-nullable*");
    }

    [Fact]
    public void SetProperties_ConversionFailure_ThrowsInvalidOperation()
    {
        var target = new SetPropsTarget();
        var properties = new Dictionary<string, object?> { ["Count"] = "not-a-number" };

        var act = () => EntityPropertyMapper.SetProperties(target, properties);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Failed to convert*");
    }

    [Fact]
    public void SetProperties_WithCallback_InvokesForEachProperty()
    {
        var target = new SetPropsTarget();
        var properties = new Dictionary<string, object?>
        {
            ["Count"] = 42,
            ["NullableName"] = "test"
        };
        var callbacks = new List<string>();

        EntityPropertyMapper.SetProperties(target, properties, name => callbacks.Add(name));

        callbacks.Should().Contain("Count");
        callbacks.Should().Contain("NullableName");
    }

    [Fact]
    public void SetProperties_NullValueWithCallback_InvokesCallback()
    {
        var target = new SetPropsTarget { NullableName = "before" };
        var properties = new Dictionary<string, object?> { ["NullableName"] = null };
        var called = false;

        EntityPropertyMapper.SetProperties(target, properties, _ => called = true);

        called.Should().BeTrue();
        target.NullableName.Should().BeNull();
    }

    public sealed class TestPropertyEntity
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public int? NullableInt { get; set; }
    }

    public sealed class SetPropsTarget
    {
        public string? NullableName { get; set; }
        public int Count { get; set; }
    }
}
