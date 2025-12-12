using FluentAssertions;
using GameGuild.Permissions;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Entities;

/// <summary>
/// Unit tests for ContentTypePermission entity
/// </summary>
public class ContentTypePermissionTests
{
    [Fact]
    public void ContentTypePermission_Should_Have_Required_Properties()
    {
        // Arrange
        var contentType = "Article";
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var permission = new ContentTypePermission
        {
            ContentType = contentType,
            UserId = userId,
            TenantId = tenantId
        };
        permission.AddPermission(PermissionType.Read);
        permission.AddPermission(PermissionType.Edit);

        // Assert
        permission.ContentType.Should().Be(contentType);
        permission.UserId.Should().Be(userId);
        permission.TenantId.Should().Be(tenantId);
        permission.HasPermission(PermissionType.Read).Should().BeTrue();
        permission.HasPermission(PermissionType.Edit).Should().BeTrue();
    }

    [Fact]
    public void ContentTypePermission_Should_Inherit_From_WithPermissions()
    {
        // Arrange & Act
        var permission = new ContentTypePermission();

        // Assert
        permission.Should().BeAssignableTo<WithPermissions>();
    }
}
