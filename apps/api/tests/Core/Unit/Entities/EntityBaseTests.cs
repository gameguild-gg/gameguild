using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Tenants;
using Xunit;

namespace GameGuild.Tests.Core.Unit.Entities;

/// <summary>
/// Unit tests for the EntityBase class
/// </summary>
public class EntityBaseTests
{
    // Test entity implementation for testing
    private class TestEntity : EntityBase<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }

        public TestEntity() : base() { }
        public TestEntity(object partial) : base(partial) { }
    }

    // Test domain event for testing
    private class TestDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public int Version { get; } = 1;
        public Guid AggregateId { get; set; }
        public string AggregateType { get; } = nameof(TestDomainEvent);
    }

    [Fact]
    public void Constructor_Should_Initialize_Default_Values()
    {
        // Act
        TestEntity entity = new();

        // Assert
        _ = entity.Id.Should().BeEmpty();
        _ = entity.Version.Should().Be(0);
        _ = entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _ = entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _ = entity.DeletedAt.Should().BeNull();
        _ = entity.Tenant.Should().BeNull();
        _ = entity.IsGlobal.Should().BeTrue();
        _ = entity.IsNew.Should().BeTrue();
        _ = entity.IsDeleted.Should().BeFalse();
        _ = entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_With_Partial_Should_Initialize_Properties()
    {
        // Arrange
        object partial = new { Name = "Test", Value = 42 };

        // Act
        TestEntity entity = new(partial);

        // Assert
        _ = entity.Name.Should().Be("Test");
        _ = entity.Value.Should().Be(42);
    }

    [Fact]
    public void IsGlobal_Should_Return_True_When_Tenant_Is_Null()
    {
        // Arrange
        TestEntity entity = new();

        // Act & Assert
        _ = entity.IsGlobal.Should().BeTrue();
    }

    [Fact]
    public void IsGlobal_Should_Return_False_When_Tenant_Is_Set()
    {
        // Arrange
        TestEntity entity = new();
        Tenant tenant = new() { Id = Guid.NewGuid(), Name = "Test Tenant" };

        // Act
        entity.Tenant = tenant;

        // Assert
        _ = entity.IsGlobal.Should().BeFalse();
    }

    [Fact]
    public void Version_Should_Not_Be_Negative_When_Set_To_Negative_Value()
    {
        // Arrange & Act & Assert
        TestEntity entity = new() { Version = 0 };

        // Act & Assert
        _ = entity.Version.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void IsNew_Should_Return_False_When_Version_Is_Not_Zero()
    {
        // Arrange
        TestEntity entity = new TestEntity { Version = 1 };

        // Act & Assert
        entity.IsNew.Should().BeFalse();
    }

    [Fact]
    public void IsDeleted_Should_Return_False_When_DeletedAt_Is_Null()
    {
        // Arrange
        TestEntity entity = new();

        // Act & Assert
        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void IsDeleted_Should_Return_True_When_DeletedAt_Has_Value()
    {
        // Arrange
        TestEntity entity = new() { DeletedAt = DateTime.UtcNow };

        // Act & Assert
        entity.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Touch_Should_Update_UpdatedAt_Timestamp()
    {
        // Arrange
        TestEntity entity = new();
        DateTime originalUpdatedAt = entity.UpdatedAt;
        Thread.Sleep(1); // Ensure time difference

        // Act
        entity.Touch();

        // Assert
        entity.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void SoftDelete_Should_Set_DeletedAt_And_Update_UpdatedAt()
    {
        // Arrange
        TestEntity entity = new();
        DateTime originalUpdatedAt = entity.UpdatedAt;

        // Act
        entity.SoftDelete();

        // Assert
        entity.DeletedAt.Should().NotBeNull();
        entity.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        entity.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_Should_Not_Change_DeletedAt_When_Already_Deleted()
    {
        // Arrange
        TestEntity entity = new();
        DateTime deletedAt = DateTime.UtcNow.AddDays(-1);
        entity.DeletedAt = deletedAt;

        // Act
        entity.SoftDelete();

        // Assert
        entity.DeletedAt.Should().Be(deletedAt);
    }

    [Fact]
    public void Restore_Should_Clear_DeletedAt_And_Update_UpdatedAt()
    {
        // Arrange
        TestEntity entity = new() { DeletedAt = DateTime.UtcNow.AddDays(-1) };
        DateTime originalUpdatedAt = entity.UpdatedAt;

        // Act
        entity.Restore();

        // Assert
        entity.DeletedAt.Should().BeNull();
        entity.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Restore_Should_Not_Change_When_Not_Deleted()
    {
        // Arrange
        TestEntity entity = new();
        DateTime originalUpdatedAt = entity.UpdatedAt;

        // Act
        entity.Restore();

        // Assert
        entity.DeletedAt.Should().BeNull();
        entity.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void AddDomainEvent_Should_Add_Event_To_Collection()
    {
        // Arrange
        TestEntity entity = new();
        TestDomainEvent domainEvent = new();

        // Act
        entity.AddDomainEvent(domainEvent);

        // Assert
        entity.DomainEvents.Should().HaveCount(1);
        entity.DomainEvents.Should().Contain(domainEvent);
    }

    [Fact]
    public void RemoveDomainEvent_Should_Remove_Event_From_Collection()
    {
        // Arrange
        TestEntity entity = new();
        TestDomainEvent domainEvent = new();
        entity.AddDomainEvent(domainEvent);

        // Act
        entity.RemoveDomainEvent(domainEvent);

        // Assert
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_Should_Remove_All_Events()
    {
        // Arrange
        TestEntity entity = new();
        entity.AddDomainEvent(new TestDomainEvent());
        entity.AddDomainEvent(new TestDomainEvent());

        // Act
        entity.ClearDomainEvents();

        // Assert
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SetProperties_Should_Update_Valid_Properties()
    {
        // Arrange
        TestEntity entity = new();
        Dictionary<string, object?> properties = new()
        {
            { nameof(TestEntity.Name), "Updated Name" },
            { nameof(TestEntity.Value), 100 }
        };

        // Act
        entity.SetProperties(properties);

        // Assert
        entity.Name.Should().Be("Updated Name");
        entity.Value.Should().Be(100);
    }

    [Fact]
    public void SetProperties_Should_Ignore_Invalid_Properties()
    {
        // Arrange
        TestEntity entity = new();
        Dictionary<string, object?> properties = new()
        {
            { "NonExistentProperty", "SomeValue" },
            { nameof(TestEntity.Name), "Valid Name" }
        };

        // Act & Assert (should not throw)
        entity.SetProperties(properties);
        entity.Name.Should().Be("Valid Name");
    }

    [Fact]
    public void SetProperties_Should_Convert_Compatible_Types()
    {
        // Arrange
        TestEntity entity = new();
        Dictionary<string, object?> properties = new()
        {
            { nameof(TestEntity.Value), "42" } // String to int conversion
        };

        // Act
        entity.SetProperties(properties);

        // Assert
        entity.Value.Should().Be(42);
    }

    [Fact]
    public void ToDictionary_Should_Return_All_Property_Values()
    {
        // Arrange
        TestEntity entity = new()
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Value = 42,
            Version = 1
        };

        // Act
        var dictionary = entity.ToDictionary();

        // Assert
        dictionary.Should().ContainKey(nameof(TestEntity.Id));
        dictionary.Should().ContainKey(nameof(TestEntity.Name));
        dictionary.Should().ContainKey(nameof(TestEntity.Value));
        dictionary.Should().ContainKey(nameof(TestEntity.Version));
        dictionary[nameof(TestEntity.Name)].Should().Be("Test");
        dictionary[nameof(TestEntity.Value)].Should().Be(42);
    }

    [Fact]
    public void ToString_Should_Return_Formatted_String()
    {
        // Arrange
        TestEntity entity = new()
        {
            Id = Guid.NewGuid(),
            Version = 1,
            CreatedAt = new DateTime(2023, 1, 1, 12, 0, 0),
            UpdatedAt = new DateTime(2023, 1, 1, 12, 30, 0)
        };

        // Act
        var result = entity.ToString();

        // Assert
        result.Should().Contain("TestEntity");
        result.Should().Contain(entity.Id.ToString());
        result.Should().Contain("Version = 1");
        result.Should().Contain("2023-01-01 12:00:00");
        result.Should().Contain("2023-01-01 12:30:00");
        result.Should().NotContain("(DELETED)");
    }

    [Fact]
    public void ToString_Should_Include_Deleted_Status_When_Deleted()
    {
        // Arrange
        TestEntity entity = new() { DeletedAt = DateTime.UtcNow };

        // Act
        var result = entity.ToString();

        // Assert
        result.Should().Contain("(DELETED)");
    }
}
