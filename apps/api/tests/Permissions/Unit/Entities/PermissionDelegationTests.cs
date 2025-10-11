using FluentAssertions;
using GameGuild.Modules.Permissions.Entities;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Entities;

/// <summary>
/// Unit tests for PermissionDelegation entity
/// </summary>
public class PermissionDelegationTests
{
    [Fact]
    public void PermissionDelegation_Should_Have_Required_Properties()
    {
        // Arrange
        var delegatorUserId = Guid.NewGuid();
        var delegateUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var permissions = new[] { PermissionType.Read, PermissionType.Edit };
        var startsAt = DateTime.UtcNow;
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var delegation = new PermissionDelegation
        {
            DelegatorUserId = delegatorUserId,
            DelegateUserId = delegateUserId,
            TenantId = tenantId,
            ResourceId = resourceId,
            DelegatedPermissions = permissions,
            StartsAt = startsAt,
            ExpiresAt = expiresAt,
            IsActive = true
        };

        // Assert
        delegation.DelegatorUserId.Should().Be(delegatorUserId);
        delegation.DelegateUserId.Should().Be(delegateUserId);
        delegation.TenantId.Should().Be(tenantId);
        delegation.ResourceId.Should().Be(resourceId);
        delegation.DelegatedPermissions.Should().BeEquivalentTo(permissions);
        delegation.StartsAt.Should().Be(startsAt);
        delegation.ExpiresAt.Should().Be(expiresAt);
        delegation.IsActive.Should().BeTrue();
    }

    [Fact]
    public void PermissionDelegation_Should_Default_DelegatedPermissions_To_Empty_Array()
    {
        // Arrange & Act
        var delegation = new PermissionDelegation();

        // Assert
        delegation.DelegatedPermissions.Should().BeEmpty();
    }

    [Fact]
    public void PermissionDelegation_Should_Default_StartsAt_To_UtcNow()
    {
        // Arrange & Act
        var delegation = new PermissionDelegation();

        // Assert
        delegation.StartsAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
