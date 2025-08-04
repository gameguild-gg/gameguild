using GameGuild.Common;
using GameGuild.Modules.Comments;
using GameGuild.Modules.Permissions;
using Moq;


namespace GameGuild.Tests.Modules.Projects.E2E.GraphQL;

/// <summary>
/// Unit tests for DAC permission checking logic in GraphQL resolvers
/// Tests the permission service integration without HTTP layer
/// </summary>
public class DACPermissionLogicTests {
    [Fact]
    public async Task CheckCommentPermissionHierarchy_WithResourcePermission_ReturnsTrue() {
        // Arrange
        var mockPermissionService = new Mock<IPermissionService>();
        mockPermissionService
            .Setup(x => x.HasResourcePermissionAsync<CommentPermission, Comment>(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<Guid>(),
                    It.IsAny<PermissionType>()
                )
            )
            .ReturnsAsync(true);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        // Act
        var result = await CheckCommentPermissionHierarchy(
            mockPermissionService.Object,
            userId,
            tenantId,
            commentId,
            PermissionType.Comment
        );

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckCommentPermissionHierarchy_WithContentTypePermission_ReturnsTrue() {
        // Arrange
        var mockPermissionService = new Mock<IPermissionService>();
        mockPermissionService
            .Setup(x => x.HasResourcePermissionAsync<CommentPermission, Comment>(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<Guid>(),
                    It.IsAny<PermissionType>()
                )
            )
            .ReturnsAsync(false);
        mockPermissionService
            .Setup(x => x.HasContentTypePermissionAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid?>(),
                    "Comment",
                    It.IsAny<PermissionType>()
                )
            )
            .ReturnsAsync(true);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        // Act
        var result = await CheckCommentPermissionHierarchy(
            mockPermissionService.Object,
            userId,
            tenantId,
            commentId,
            PermissionType.Comment
        );

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckCommentPermissionHierarchy_WithTenantPermission_ReturnsTrue() {
        // Arrange
        var mockPermissionService = new Mock<IPermissionService>();
        mockPermissionService
            .Setup(x => x.HasResourcePermissionAsync<CommentPermission, Comment>(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<Guid>(),
                    It.IsAny<PermissionType>()
                )
            )
            .ReturnsAsync(false);
        mockPermissionService
            .Setup(x => x.HasContentTypePermissionAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid?>(),
                    "Comment",
                    It.IsAny<PermissionType>()
                )
            )
            .ReturnsAsync(false);
        mockPermissionService
            .Setup(x => x.HasTenantPermissionAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<PermissionType>()))
            .ReturnsAsync(true);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        // Act
        var result = await CheckCommentPermissionHierarchy(
            mockPermissionService.Object,
            userId,
            tenantId,
            commentId,
            PermissionType.Comment
        );

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckCommentPermissionHierarchy_WithNoPermissions_ReturnsFalse() {
        // Arrange
        var mockPermissionService = new Mock<IPermissionService>();
        mockPermissionService
            .Setup(x => x.HasResourcePermissionAsync<CommentPermission, Comment>(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<Guid>(),
                    It.IsAny<PermissionType>()
                )
            )
            .ReturnsAsync(false);
        mockPermissionService
            .Setup(x => x.HasContentTypePermissionAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid?>(),
                    "Comment",
                    It.IsAny<PermissionType>()
                )
            )
            .ReturnsAsync(false);
        mockPermissionService
            .Setup(x => x.HasTenantPermissionAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<PermissionType>()))
            .ReturnsAsync(false);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        // Act
        var result = await CheckCommentPermissionHierarchy(
            mockPermissionService.Object,
            userId,
            tenantId,
            commentId,
            PermissionType.Comment
        );

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Copy of the hierarchical permission check method from CommentQuery
    /// This allows us to unit test the logic independently
    /// </summary>
    private static async Task<bool> CheckCommentPermissionHierarchy(
        IPermissionService permissionService, Guid userId,
        Guid tenantId, Guid commentId, PermissionType permission
    ) {
        try {
            // 1. Check resource-level permission
            var hasResourcePermission =
                await permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
                    userId,
                    tenantId,
                    commentId,
                    permission
                );

            if (hasResourcePermission) return true;
        }
        catch {
            // Continue to fallbacks if resource checking fails
        }

        // 2. Check content-type permission
        var hasContentTypePermission =
            await permissionService.HasContentTypePermissionAsync(userId, tenantId, "Comment", permission);

        if (hasContentTypePermission) return true;

        // 3. Check tenant permission
        var hasTenantPermission = await permissionService.HasTenantPermissionAsync(userId, tenantId, permission);

        return hasTenantPermission;
    }
}