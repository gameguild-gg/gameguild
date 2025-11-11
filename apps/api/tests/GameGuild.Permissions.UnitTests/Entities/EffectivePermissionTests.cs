using FluentAssertions;
using GameGuild.Core.Domain.Permissions;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Entities;

/// <summary>
/// Unit tests for EffectivePermission entity
/// </summary>
public class EffectivePermissionTests
{
    [Fact]
    public void EffectivePermission_Should_Have_Required_Properties()
    {
        // Arrange
        var permission = PermissionType.Read;
        var isGranted = true;
        var source = PermissionSource.TenantUser;
        var sourceDescription = "Direct permission assignment";
        var grantedBy = "admin@example.com";
        var grantedAt = DateTime.UtcNow;
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var isInherited = false;
        var isExplicit = true;
        var priority = 10;

        // Act
        var effectivePermission = new EffectivePermission
        {
            Permission = permission,
            IsGranted = isGranted,
            Source = source,
            SourceDescription = sourceDescription,
            GrantedBy = grantedBy,
            GrantedAt = grantedAt,
            ExpiresAt = expiresAt,
            IsInherited = isInherited,
            IsExplicit = isExplicit,
            Priority = priority
        };

        // Assert
        effectivePermission.Permission.Should().Be(permission);
        effectivePermission.IsGranted.Should().Be(isGranted);
        effectivePermission.Source.Should().Be(source);
        effectivePermission.SourceDescription.Should().Be(sourceDescription);
        effectivePermission.GrantedBy.Should().Be(grantedBy);
        effectivePermission.GrantedAt.Should().Be(grantedAt);
        effectivePermission.ExpiresAt.Should().Be(expiresAt);
        effectivePermission.IsInherited.Should().Be(isInherited);
        effectivePermission.IsExplicit.Should().Be(isExplicit);
        effectivePermission.Priority.Should().Be(priority);
    }

    [Fact]
    public void EffectivePermission_Should_Default_SourceDescription_To_Empty_String()
    {
        // Arrange & Act
        var effectivePermission = new EffectivePermission();

        // Assert
        effectivePermission.SourceDescription.Should().BeEmpty();
    }
}
