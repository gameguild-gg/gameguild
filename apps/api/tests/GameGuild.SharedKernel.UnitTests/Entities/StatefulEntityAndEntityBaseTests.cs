using FluentAssertions;
using GameGuild.Entities;
using GameGuild.SharedKernel;
using Xunit;

namespace GameGuild.Tests.SharedKernel.Unit.Entities;

/// <summary>
/// Unit tests for StatefulEntity base class
/// </summary>
public class StatefulEntityTests
{
    // Test concrete implementation for testing
    private enum TestStatus
    {
        Initial,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    private class TestStatefulEntity : StatefulEntity<TestStatus>
    {
        protected override IReadOnlyDictionary<TestStatus, IReadOnlySet<TestStatus>> ValidTransitions { get; } =
            new Dictionary<TestStatus, IReadOnlySet<TestStatus>>
            {
                { TestStatus.Initial, new HashSet<TestStatus> { TestStatus.Processing, TestStatus.Cancelled } },
                { TestStatus.Processing, new HashSet<TestStatus> { TestStatus.Completed, TestStatus.Failed } },
                { TestStatus.Completed, new HashSet<TestStatus>() },
                { TestStatus.Failed, new HashSet<TestStatus> { TestStatus.Initial } },
                { TestStatus.Cancelled, new HashSet<TestStatus>() }
            };

        public override TestStatus Status { get; protected set; } = TestStatus.Initial;

        public void SetStatus(TestStatus status) => TransitionTo(status);

        public (TestStatus OldStatus, TestStatus NewStatus)? LastStatusChange { get; private set; }

        protected override void OnStatusChanged(TestStatus oldStatus, TestStatus newStatus)
        {
            LastStatusChange = (oldStatus, newStatus);
        }
    }

    [Fact]
    public void CanTransitionTo_WhenTransitionIsValid_ShouldReturnTrue()
    {
        // Arrange
        var entity = new TestStatefulEntity();

        // Act & Assert
        entity.CanTransitionTo(TestStatus.Processing).Should().BeTrue();
        entity.CanTransitionTo(TestStatus.Cancelled).Should().BeTrue();
    }

    [Fact]
    public void CanTransitionTo_WhenTransitionIsInvalid_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestStatefulEntity();

        // Act & Assert
        entity.CanTransitionTo(TestStatus.Completed).Should().BeFalse();
        entity.CanTransitionTo(TestStatus.Failed).Should().BeFalse();
    }

    [Fact]
    public void TransitionTo_WhenTransitionIsValid_ShouldChangeStatus()
    {
        // Arrange
        var entity = new TestStatefulEntity();

        // Act
        entity.SetStatus(TestStatus.Processing);

        // Assert
        entity.Status.Should().Be(TestStatus.Processing);
    }

    [Fact]
    public void TransitionTo_WhenTransitionIsValid_ShouldCallOnStatusChanged()
    {
        // Arrange
        var entity = new TestStatefulEntity();

        // Act
        entity.SetStatus(TestStatus.Processing);

        // Assert
        entity.LastStatusChange.Should().NotBeNull();
        entity.LastStatusChange!.Value.OldStatus.Should().Be(TestStatus.Initial);
        entity.LastStatusChange!.Value.NewStatus.Should().Be(TestStatus.Processing);
    }

    [Fact]
    public void TransitionTo_WhenTransitionIsInvalid_ShouldThrowInvalidStateTransitionException()
    {
        // Arrange
        var entity = new TestStatefulEntity();

        // Act
        var act = () => entity.SetStatus(TestStatus.Completed);

        // Assert
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void TransitionTo_FromTerminalState_ShouldThrowInvalidStateTransitionException()
    {
        // Arrange
        var entity = new TestStatefulEntity();
        entity.SetStatus(TestStatus.Processing);
        entity.SetStatus(TestStatus.Completed);

        // Act - Completed is a terminal state
        var act = () => entity.SetStatus(TestStatus.Processing);

        // Assert
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void TransitionTo_FromFailedState_ShouldAllowRetry()
    {
        // Arrange
        var entity = new TestStatefulEntity();
        entity.SetStatus(TestStatus.Processing);
        entity.SetStatus(TestStatus.Failed);

        // Act - Failed can transition back to Initial for retry
        entity.SetStatus(TestStatus.Initial);

        // Assert
        entity.Status.Should().Be(TestStatus.Initial);
    }

    [Fact]
    public void MultipleValidTransitions_ShouldWorkCorrectly()
    {
        // Arrange
        var entity = new TestStatefulEntity();

        // Act - Complete path: Initial -> Processing -> Completed
        entity.SetStatus(TestStatus.Processing);
        entity.SetStatus(TestStatus.Completed);

        // Assert
        entity.Status.Should().Be(TestStatus.Completed);
    }
}

/// <summary>
/// Unit tests for EntityBase class
/// </summary>
public class EntityBaseTests
{
    private class TestEntity : EntityBase
    {
    }

    [Fact]
    public void DefaultConstructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().NotBe(Guid.Empty);
        entity.Version.Should().Be(0);
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.DeletedAt.Should().BeNull();
        entity.TenantId.Should().BeNull();
    }

    [Fact]
    public void IsNew_WhenVersionIsZero_ShouldReturnTrue()
    {
        // Arrange
        var entity = new TestEntity();

        // Assert
        entity.IsNew.Should().BeTrue();
    }

    [Fact]
    public void IsNew_WhenVersionIsGreaterThanZero_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestEntity();
        entity.Version = 1;

        // Assert
        entity.IsNew.Should().BeFalse();
    }

    [Fact]
    public void IsDeleted_WhenDeletedAtIsNull_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestEntity();

        // Assert
        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void IsDeleted_WhenDeletedAtIsSet_ShouldReturnTrue()
    {
        // Arrange
        var entity = new TestEntity();
        entity.SoftDelete();

        // Assert
        entity.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void IsGlobal_WhenTenantIdIsNull_ShouldReturnTrue()
    {
        // Arrange
        var entity = new TestEntity();

        // Assert
        entity.IsGlobal.Should().BeTrue();
    }

    [Fact]
    public void Touch_ShouldUpdateUpdatedAtTimestamp()
    {
        // Arrange
        var entity = new TestEntity();
        var originalUpdatedAt = entity.UpdatedAt;
        
        // Small delay to ensure timestamp difference
        Thread.Sleep(10);

        // Act
        entity.Touch();

        // Assert
        entity.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void SoftDelete_ShouldSetDeletedAtAndTouch()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.SoftDelete();

        // Assert
        entity.DeletedAt.Should().NotBeNull();
        entity.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldNotUpdateTimestamp()
    {
        // Arrange
        var entity = new TestEntity();
        entity.SoftDelete();
        var originalDeletedAt = entity.DeletedAt;
        
        Thread.Sleep(10);

        // Act
        entity.SoftDelete();

        // Assert
        entity.DeletedAt.Should().Be(originalDeletedAt);
    }

    [Fact]
    public void Restore_ShouldClearDeletedAtAndTouch()
    {
        // Arrange
        var entity = new TestEntity();
        entity.SoftDelete();
        var updatedAtAfterDelete = entity.UpdatedAt;
        
        Thread.Sleep(10);

        // Act
        entity.Restore();

        // Assert
        entity.DeletedAt.Should().BeNull();
        entity.IsDeleted.Should().BeFalse();
        entity.UpdatedAt.Should().BeAfter(updatedAtAfterDelete);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldNotUpdateTimestamp()
    {
        // Arrange
        var entity = new TestEntity();
        var originalUpdatedAt = entity.UpdatedAt;
        
        Thread.Sleep(10);

        // Act
        entity.Restore();

        // Assert
        entity.DeletedAt.Should().BeNull();
        entity.UpdatedAt.Should().BeCloseTo(originalUpdatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void DomainEvents_ShouldBeInitiallyEmpty()
    {
        // Arrange
        var entity = new TestEntity();

        // Assert
        entity.DomainEvents.Should().NotBeNull().And.BeEmpty();
    }
}
