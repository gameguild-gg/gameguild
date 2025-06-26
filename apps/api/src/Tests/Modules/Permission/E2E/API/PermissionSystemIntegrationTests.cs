using Xunit;
using Microsoft.EntityFrameworkCore;
using GameGuild.Data;
using GameGuild.Common.Services;
using GameGuild.Common.Entities;
using GameGuild.Modules.Tenant.Models;
using GameGuild.Modules.Comment.Models;
using GameGuild.Tests.Fixtures;

namespace GameGuild.Tests.Modules.Permission.E2E.API;

/// <summary>
/// Integration tests for the complete Permission System
/// Tests hierarchical permission resolution and cross-layer interactions
/// </summary>
public class PermissionSystemIntegrationTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    private readonly IServiceScope _scope;

    private readonly ApplicationDbContext _context;

    private readonly IPermissionService _permissionService;

    public PermissionSystemIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();

        // Use a separate in-memory database for integration tests
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        );
        services.AddScoped<IPermissionService, PermissionService>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        _context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        _permissionService = serviceProvider.GetRequiredService<IPermissionService>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _context.Dispose();
    }

    #region Hierarchical Permission Resolution Tests

    [Fact]
    public async Task PermissionHierarchy_ResourceOverridesContentType_CorrectResolution()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var contentType = "Comment";

        // Set content-type permission (Allow Read)
        await _permissionService.GrantContentTypePermissionAsync(userId, tenantId, contentType, [PermissionType.Read]);

        // Set resource-specific permission (Deny by not granting Read, but grant Edit)
        await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(userId, tenantId, resourceId, [PermissionType.Edit]);

        // Act
        bool hasContentTypeRead = await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, PermissionType.Read);
        bool hasResourceRead = await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(userId, tenantId, resourceId, PermissionType.Read);
        bool hasResourceEdit = await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(userId, tenantId, resourceId, PermissionType.Edit);

        // Assert
        Assert.True(hasContentTypeRead); // Content-type level allows Read
        Assert.False(hasResourceRead); // Resource level doesn't grant Read (override)
        Assert.True(hasResourceEdit); // Resource level grants Edit
    }

    [Fact]
    public async Task PermissionHierarchy_ContentTypeOverridesTenant_CorrectResolution()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var contentType = "Comment";

        // Set tenant-wide permission (Allow Read and Edit)
        await _permissionService.GrantTenantPermissionAsync(userId, tenantId, [PermissionType.Read, PermissionType.Edit]);

        // Set content-type permission (Only Read, no Edit)
        await _permissionService.GrantContentTypePermissionAsync(userId, tenantId, contentType, [PermissionType.Read]);

        // Act
        bool hasTenantRead = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read);
        bool hasTenantEdit = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Edit);
        bool hasContentTypeRead = await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, PermissionType.Read);
        bool hasContentTypeEdit = await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, PermissionType.Edit);

        // Assert
        Assert.True(hasTenantRead); // Tenant level allows Read
        Assert.True(hasTenantEdit); // Tenant level allows Edit
        Assert.True(hasContentTypeRead); // Content-type allows Read (inherited + explicit)
        Assert.True(hasContentTypeEdit); // Content-type inherits Edit from tenant level
    }

    [Fact]
    public async Task PermissionHierarchy_DefaultFallthrough_GlobalToTenantToUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Set global default permissions
        await _permissionService.GrantTenantPermissionAsync(userId: null, tenantId: null, [PermissionType.Read]);

        // Set tenant default permissions
        await _permissionService.GrantTenantPermissionAsync(userId: null, tenantId, [PermissionType.Comment]);

        // User has no specific permissions

        // Act
        bool hasRead = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read);
        bool hasComment = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Comment);
        bool hasEdit = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Edit);

        // Assert
        Assert.True(hasRead); // Should inherit from global default
        Assert.True(hasComment); // Should inherit from tenant default
        Assert.False(hasEdit); // Not granted at any level
    }

    [Fact]
    public async Task PermissionHierarchy_UserSpecificOverridesDefaults_CorrectPrecedence()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Set global default permissions
        await _permissionService.GrantTenantPermissionAsync(userId: null, tenantId: null, [PermissionType.Read, PermissionType.Comment]);

        // Set tenant default permissions
        await _permissionService.GrantTenantPermissionAsync(userId: null, tenantId, [PermissionType.Read, PermissionType.Edit]);

        // Set user-specific permissions (different set)
        await _permissionService.GrantTenantPermissionAsync(userId, tenantId, [PermissionType.Vote]);

        // Act
        bool hasRead = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read);
        bool hasComment = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Comment);
        bool hasEdit = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Edit);
        bool hasVote = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Vote);

        // Assert
        Assert.True(hasVote); // User-specific permission
        Assert.True(hasRead); // Should fall through to tenant default
        Assert.True(hasComment); // Should fall through to global default
        Assert.True(hasEdit); // Should fall through to tenant default
    }

    #endregion

    #region Cross-Layer Permission Scenarios

    [Fact]
    public async Task CrossLayerScenario_BlogPostWorkflow_CompletePermissionFlow()
    {
        // Arrange - Blog post editing workflow
        var authorId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var readerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var contentType = "Article";

        // Set global defaults (everyone can read)
        await _permissionService.GrantTenantPermissionAsync(userId: null, tenantId: null, [PermissionType.Read]);

        // Set tenant defaults for Article content type (basic commenting)
        await _permissionService.GrantContentTypePermissionAsync(userId: null, tenantId, contentType, [PermissionType.Comment]);

        // Author: tenant-wide permissions for content creation
        await _permissionService.GrantTenantPermissionAsync(authorId, tenantId, [PermissionType.Draft, PermissionType.Submit]);

        // Author: content-type permissions for articles
        await _permissionService.GrantContentTypePermissionAsync(authorId, tenantId, contentType, [PermissionType.Edit, PermissionType.Delete]);

        // Editor: content-type permissions for review
        await _permissionService.GrantContentTypePermissionAsync(editorId, tenantId, contentType, [PermissionType.Review, PermissionType.Approve, PermissionType.Publish]);

        // Admin: tenant-wide admin permissions
        await _permissionService.GrantTenantPermissionAsync(adminId, tenantId, [PermissionType.Review, PermissionType.Ban, PermissionType.HardDelete]);

        // Act & Assert - Test various permission scenarios

        // Everyone can read (global default)
        Assert.True(await _permissionService.HasTenantPermissionAsync(authorId, tenantId, PermissionType.Read));
        Assert.True(await _permissionService.HasTenantPermissionAsync(editorId, tenantId, PermissionType.Read));
        Assert.True(await _permissionService.HasTenantPermissionAsync(readerId, tenantId, PermissionType.Read));
        Assert.True(await _permissionService.HasTenantPermissionAsync(adminId, tenantId, PermissionType.Read));

        // Everyone can comment on articles (content-type default)
        Assert.True(await _permissionService.HasContentTypePermissionAsync(authorId, tenantId, contentType, PermissionType.Comment));
        Assert.True(await _permissionService.HasContentTypePermissionAsync(editorId, tenantId, contentType, PermissionType.Comment));
        Assert.True(await _permissionService.HasContentTypePermissionAsync(readerId, tenantId, contentType, PermissionType.Comment));
        Assert.True(await _permissionService.HasContentTypePermissionAsync(adminId, tenantId, contentType, PermissionType.Comment));

        // Only author can create drafts
        Assert.True(await _permissionService.HasTenantPermissionAsync(authorId, tenantId, PermissionType.Draft));
        Assert.False(await _permissionService.HasTenantPermissionAsync(editorId, tenantId, PermissionType.Draft));
        Assert.False(await _permissionService.HasTenantPermissionAsync(readerId, tenantId, PermissionType.Draft));

        // Only author can edit articles
        Assert.True(await _permissionService.HasContentTypePermissionAsync(authorId, tenantId, contentType, PermissionType.Edit));
        Assert.False(await _permissionService.HasContentTypePermissionAsync(editorId, tenantId, contentType, PermissionType.Edit));
        Assert.False(await _permissionService.HasContentTypePermissionAsync(readerId, tenantId, contentType, PermissionType.Edit));

        // Only editor can approve articles
        Assert.False(await _permissionService.HasContentTypePermissionAsync(authorId, tenantId, contentType, PermissionType.Approve));
        Assert.True(await _permissionService.HasContentTypePermissionAsync(editorId, tenantId, contentType, PermissionType.Approve));
        Assert.False(await _permissionService.HasContentTypePermissionAsync(readerId, tenantId, contentType, PermissionType.Approve));

        // Only admin can ban users
        Assert.False(await _permissionService.HasTenantPermissionAsync(authorId, tenantId, PermissionType.Ban));
        Assert.False(await _permissionService.HasTenantPermissionAsync(editorId, tenantId, PermissionType.Ban));
        Assert.False(await _permissionService.HasTenantPermissionAsync(readerId, tenantId, PermissionType.Ban));
        Assert.True(await _permissionService.HasTenantPermissionAsync(adminId, tenantId, PermissionType.Ban));
    }

    [Fact]
    public async Task CrossLayerScenario_CommentModeration_ResourceLevelOverrides()
    {
        // Arrange - Comment moderation with specific comment permissions
        var userId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var sensitiveCommentId = Guid.NewGuid();
        var contentType = "Comment";

        // Set content-type permissions for all comments
        await _permissionService.GrantContentTypePermissionAsync(moderatorId, tenantId, contentType, [PermissionType.Review, PermissionType.Hide]);

        // Grant special permissions for sensitive comment
        await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(moderatorId, tenantId, sensitiveCommentId, [PermissionType.Delete, PermissionType.Ban]);

        // Act & Assert

        // Moderator can review any comment (content-type level)
        Assert.True(await _permissionService.HasContentTypePermissionAsync(moderatorId, tenantId, contentType, PermissionType.Review));

        // Moderator can hide any comment (content-type level)
        Assert.True(await _permissionService.HasContentTypePermissionAsync(moderatorId, tenantId, contentType, PermissionType.Hide));

        // Moderator cannot delete regular comments (no content-type permission)
        Assert.False(await _permissionService.HasContentTypePermissionAsync(moderatorId, tenantId, contentType, PermissionType.Delete));

        // Moderator cannot delete regular comment at resource level (no resource permission)
        Assert.False(await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(moderatorId, tenantId, commentId, PermissionType.Delete));

        // Moderator CAN delete sensitive comment (resource-specific permission)
        Assert.True(await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(moderatorId, tenantId, sensitiveCommentId, PermissionType.Delete));

        // Moderator CAN ban user from sensitive comment (resource-specific permission)
        Assert.True(await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(moderatorId, tenantId, sensitiveCommentId, PermissionType.Ban));
    }

    #endregion

    #region Multi-Tenant Permission Isolation

    [Fact]
    public async Task MultiTenant_PermissionIsolation_CrossTenantSecurity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        var contentType = "Article";
        var resource1Id = Guid.NewGuid();
        var resource2Id = Guid.NewGuid();

        // Grant permissions in tenant 1
        await _permissionService.GrantTenantPermissionAsync(userId, tenant1Id, [PermissionType.Read, PermissionType.Edit]);
        await _permissionService.GrantContentTypePermissionAsync(userId, tenant1Id, contentType, [PermissionType.Delete]);
        await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(userId, tenant1Id, resource1Id, [PermissionType.Review]);

        // Grant different permissions in tenant 2
        await _permissionService.GrantTenantPermissionAsync(userId, tenant2Id, [PermissionType.Read]);
        await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(userId, tenant2Id, resource2Id, [PermissionType.Comment]);

        // Act & Assert - Verify tenant isolation

        // Tenant 1 permissions
        Assert.True(await _permissionService.HasTenantPermissionAsync(userId, tenant1Id, PermissionType.Edit));
        Assert.True(await _permissionService.HasContentTypePermissionAsync(userId, tenant1Id, contentType, PermissionType.Delete));
        Assert.True(await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(userId, tenant1Id, resource1Id, PermissionType.Review));

        // Tenant 2 should NOT have tenant 1 permissions
        Assert.False(await _permissionService.HasTenantPermissionAsync(userId, tenant2Id, PermissionType.Edit));
        Assert.False(await _permissionService.HasContentTypePermissionAsync(userId, tenant2Id, contentType, PermissionType.Delete));
        Assert.False(await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(userId, tenant2Id, resource1Id, PermissionType.Review));

        // Tenant 1 should NOT have tenant 2 specific permissions
        Assert.False(await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(userId, tenant1Id, resource2Id, PermissionType.Comment));

        // Both tenants should have Read (granted to both)
        Assert.True(await _permissionService.HasTenantPermissionAsync(userId, tenant1Id, PermissionType.Read));
        Assert.True(await _permissionService.HasTenantPermissionAsync(userId, tenant2Id, PermissionType.Read));
    }

    #endregion

    #region Permission Expiration and Lifecycle

    [Fact]
    public async Task PermissionLifecycle_ExpirationAndReactivation_WorksCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Grant membership with minimal permissions
        TenantPermission membership = await _permissionService.JoinTenantAsync(userId, tenantId);
        Assert.True(membership.IsActiveMembership);

        // Grant additional permissions
        await _permissionService.GrantTenantPermissionAsync(userId, tenantId, [PermissionType.Edit, PermissionType.Delete]);

        // Verify active permissions
        Assert.True(await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Edit));
        Assert.True(await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Delete));

        // Act - Leave tenant (expire membership)
        await _permissionService.LeaveTenantAsync(userId, tenantId);

        // Assert - Permissions should be inactive
        Assert.False(await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Edit));
        Assert.False(await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Delete));
        Assert.False(await _permissionService.IsUserInTenantAsync(userId, tenantId));

        // Act - Rejoin tenant
        TenantPermission rejoinedMembership = await _permissionService.JoinTenantAsync(userId, tenantId);

        // Assert - Should reactivate with minimal permissions only
        Assert.True(rejoinedMembership.IsActiveMembership);
        Assert.True(await _permissionService.IsUserInTenantAsync(userId, tenantId));

        // Should have minimal permissions
        foreach (PermissionType permission in TenantPermissionConstants.MinimalUserPermissions)
        {
            Assert.True(await _permissionService.HasTenantPermissionAsync(userId, tenantId, permission));
        }

        // Should NOT have the previously granted additional permissions
        Assert.False(await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Edit));
        Assert.False(await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Delete));
    }

    #endregion

    #region Effective Permissions Integration

    [Fact]
    public async Task EffectivePermissions_ComplexHierarchy_ReturnsCorrectUnion()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Set up complex permission hierarchy
        await _permissionService.GrantTenantPermissionAsync(userId: null, tenantId: null, [PermissionType.Read]); // Global default
        await _permissionService.GrantTenantPermissionAsync(userId: null, tenantId, [PermissionType.Comment, PermissionType.Vote]); // Tenant default
        await _permissionService.GrantTenantPermissionAsync(userId, tenantId, [PermissionType.Edit, PermissionType.Share]); // User specific

        // Act
        var effectivePermissions = await _permissionService.GetEffectiveTenantPermissionsAsync(userId, tenantId);

        // Assert
        var permissions = effectivePermissions.ToArray();

        // Should include permissions from all levels
        Assert.Contains(PermissionType.Read, permissions); // From global default
        Assert.Contains(PermissionType.Comment, permissions); // From tenant default
        Assert.Contains(PermissionType.Vote, permissions); // From tenant default
        Assert.Contains(PermissionType.Edit, permissions); // From user specific
        Assert.Contains(PermissionType.Share, permissions); // From user specific

        // Should not include ungranted permissions
        Assert.DoesNotContain(PermissionType.Delete, permissions);
        Assert.DoesNotContain(PermissionType.Ban, permissions);

        // Verify count (no duplicates)
        Assert.Equal(5, permissions.Length);
    }

    #endregion

    #region Bulk Operations Integration

    [Fact]
    public async Task BulkOperations_MultipleResourcesAndUsers_PerformanceAndCorrectness()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var resourceIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToArray();

        // Grant permissions to various resources for both users
        for (var i = 0; i < resourceIds.Length; i++)
        {
            if (i % 2 == 0) // Even resources for user1
            {
                await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
                    user1Id,
                    tenantId,
                    resourceIds[i],
                    [PermissionType.Read, PermissionType.Edit]
                );
            }
            if (i % 3 == 0) // Every third resource for user2
            {
                await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
                    user2Id,
                    tenantId,
                    resourceIds[i],
                    [PermissionType.Read, PermissionType.Delete]
                );
            }
        }

        // Act
        var user1Permissions = await _permissionService.GetBulkResourcePermissionsAsync<CommentPermission, Comment>(user1Id, tenantId, resourceIds);
        var user2Permissions = await _permissionService.GetBulkResourcePermissionsAsync<CommentPermission, Comment>(user2Id, tenantId, resourceIds);

        // Assert
        // User 1 should have permissions for 5 resources (even indices: 0, 2, 4, 6, 8)
        Assert.Equal(5, user1Permissions.Count);
        for (var i = 0; i < resourceIds.Length; i += 2)
        {
            Assert.True(user1Permissions.ContainsKey(resourceIds[i]));
            var permissions = user1Permissions[resourceIds[i]].ToArray();
            Assert.Contains(PermissionType.Read, permissions);
            Assert.Contains(PermissionType.Edit, permissions);
        }

        // User 2 should have permissions for 4 resources (indices divisible by 3: 0, 3, 6, 9)
        Assert.Equal(4, user2Permissions.Count);
        for (var i = 0; i < resourceIds.Length; i += 3)
        {
            Assert.True(user2Permissions.ContainsKey(resourceIds[i]));
            var permissions = user2Permissions[resourceIds[i]].ToArray();
            Assert.Contains(PermissionType.Read, permissions);
            Assert.Contains(PermissionType.Delete, permissions);
        }
    }

    #endregion
}
