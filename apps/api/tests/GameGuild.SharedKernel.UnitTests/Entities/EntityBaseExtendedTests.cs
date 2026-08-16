using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.CQRS.Models;

namespace GameGuild.SharedKernel.UnitTests.Entities;

public class EntityBaseDomainEventsTests
{
    [Fact]
    public void AddDomainEvent_ShouldBeRetrieable()
    {
        var entity = new TestEntity();
        var evt = new TestDomainEvent("test");

        entity.AddDomainEvent(evt);

        entity.DomainEvents.Should().ContainSingle().Which.Should().BeSameAs(evt);
    }

    [Fact]
    public void AddMultipleDomainEvents_ShouldAccumulate()
    {
        var entity = new TestEntity();

        entity.AddDomainEvent(new TestDomainEvent("one"));
        entity.AddDomainEvent(new TestDomainEvent("two"));
        entity.AddDomainEvent(new TestDomainEvent("three"));

        entity.DomainEvents.Should().HaveCount(3);
    }

    [Fact]
    public void RemoveDomainEvent_ShouldRemoveSpecificEvent()
    {
        var entity = new TestEntity();
        var evt1 = new TestDomainEvent("one");
        var evt2 = new TestDomainEvent("two");
        entity.AddDomainEvent(evt1);
        entity.AddDomainEvent(evt2);

        entity.RemoveDomainEvent(evt1);

        entity.DomainEvents.Should().ContainSingle().Which.Should().BeSameAs(evt2);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAll()
    {
        var entity = new TestEntity();
        entity.AddDomainEvent(new TestDomainEvent("one"));
        entity.AddDomainEvent(new TestDomainEvent("two"));

        entity.ClearDomainEvents();

        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_ShouldBeReadOnly()
    {
        var entity = new TestEntity();

        entity.DomainEvents.Should().BeAssignableTo<IReadOnlyList<IDomainEvent>>();
    }
}

public class EntityBaseSetTenantIdTests
{
    [Fact]
    public void SetTenantId_WithGuid_ShouldSetTenantId()
    {
        var entity = new TestEntity();
        var tenantId = Guid.NewGuid();

        entity.SetTenantId(tenantId);

        entity.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void SetTenantId_WithTenantIdObject_ShouldSetValue()
    {
        var entity = new TestEntity();
        var tenantId = new TenantId(Guid.NewGuid());

        entity.SetTenantId(tenantId);

        entity.TenantId.Should().Be(tenantId.Value);
    }

    [Fact]
    public void SetTenantId_DefaultTenantId_ShouldSetEmptyGuid()
    {
        var entity = new TestEntity();

        entity.SetTenantId(default(TenantId));

        entity.TenantId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void IsGlobal_WhenNoTenant_ShouldBeTrue()
    {
        var entity = new TestEntity();

        entity.IsGlobal.Should().BeTrue();
    }

    [Fact]
    public void IsGlobal_AfterSettingTenant_ShouldBeFalse()
    {
        var entity = new TestEntity();
        entity.SetTenantId(Guid.NewGuid());

        entity.IsGlobal.Should().BeFalse();
    }
}

public class EntityBaseToStringTests
{
    [Fact]
    public void ToString_ShouldContainTypeName()
    {
        var entity = new TestEntity();

        entity.ToString().Should().Contain("TestEntity");
    }

    [Fact]
    public void ToString_ShouldContainId()
    {
        var entity = new TestEntity();

        entity.ToString().Should().Contain(entity.Id.ToString());
    }

    [Fact]
    public void ToString_ShouldContainVersion()
    {
        var entity = new TestEntity();

        entity.ToString().Should().Contain("Version = 0");
    }

    [Fact]
    public void ToString_WhenNotDeleted_ShouldNotContainDeleted()
    {
        var entity = new TestEntity();

        entity.ToString().Should().NotContain("DELETED");
    }
}

public class EntityBaseFactoryTests
{
    [Fact]
    public void PartialConstructor_WithNull_DoesNotApplyProperties()
    {
        var entity = new TestEntity(null!);

        entity.Name.Should().BeEmpty();
        entity.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_Generic_ShouldReturnInstance()
    {
        var entity = EntityBase.Create<TestEntity>();

        entity.Should().NotBeNull();
        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithPartial_ShouldApplyProperties()
    {
        var entity = EntityBase.Create<TestEntity>(new { Name = "Test" });

        entity.Should().NotBeNull();
        entity.Name.Should().Be("Test");
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        var entity1 = EntityBase.Create<TestEntity>();
        var entity2 = EntityBase.Create<TestEntity>();

        entity1.Id.Should().NotBe(entity2.Id);
    }
}

public class EntityBaseSetPropertiesTests
{
    [Fact]
    public void SetProperties_ShouldUpdateMatchingProperties()
    {
        var entity = new TestEntity();

        entity.SetProperties(new Dictionary<string, object?>
        {
            { "Name", "Updated" }
        });

        entity.Name.Should().Be("Updated");
    }

    [Fact]
    public void SetProperties_ShouldTouchUpdatedAt()
    {
        var entity = new TestEntity();
        var originalUpdatedAt = entity.UpdatedAt;

        // Small delay to ensure different timestamp
        System.Threading.Thread.Sleep(10);
        entity.SetProperties(new Dictionary<string, object?>
        {
            { "Name", "Updated" }
        });

        entity.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void SetProperties_WithUnknownProperty_ShouldNotThrow()
    {
        var entity = new TestEntity();

        var act = () => entity.SetProperties(new Dictionary<string, object?>
        {
            { "NonExistentProperty", "value" }
        });

        act.Should().NotThrow();
    }
}

/// <summary>Test entity for unit testing EntityBase functionality</summary>
public class TestEntity : EntityBase
{
    public TestEntity() { }

    public TestEntity(object partial) : base(partial) { }

    public string Name { get; set; } = string.Empty;
}

/// <summary>Test domain event</summary>
public class TestDomainEvent : DomainEvent
{
    public string Message { get; }
    public TestDomainEvent(string message) { Message = message; }
}
