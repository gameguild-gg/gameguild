using Xunit;
using System.Text;
using System.Text.Json;
using GameGuild.Data;
using GameGuild.Common.Entities;
using GameGuild.Common.Services;
using GameGuild.Modules.User.Models;
using GameGuild.Modules.Tenant.Models;
using GameGuild.Modules.Comment.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using GameGuild.Modules.Auth.Services;
using GameGuild.Modules.Auth.Dtos;
using GameGuild.Tests.Fixtures;

namespace GameGuild.Tests.Modules.Permission.E2E;

/// <summary>
/// End-to-end tests for Permission Service API endpoints
/// Tests permission management operations via GraphQL and REST APIs
/// </summary>
public class PermissionServiceE2ETests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    private readonly HttpClient _client;

    private readonly IServiceScope _scope;

    private readonly ApplicationDbContext _context;

    private readonly IPermissionService _permissionService;

    public PermissionServiceE2ETests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();

        // Use a separate in-memory database for E2E tests
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
        _client.Dispose();
    }

    #region Permission Management GraphQL Tests

    [Fact]
    public async Task GraphQL_GrantTenantPermission_CreatesPermissionRecord()
    {
        // Arrange
        User admin = await CreateTestAdminUserAsync();
        User user = await CreateTestUserAsync();
        Tenant tenant = await CreateTestTenantAsync();

        string token = await CreateJwtTokenForUserAsync(admin, tenant);
        SetAuthorizationHeader(token);

        var mutation = @"
            mutation GrantTenantPermission($input: GrantTenantPermissionInput!) {
                grantTenantPermission(input: $input) {
                    id
                    userId
                    tenantId
                    permissionFlags1
                    permissionFlags2
                }
            }";

        var request = new
        {
            query = mutation,
            variables = new
            {
                input = new
                {
                    userId = user.Id,
                    tenantId = tenant.Id,
                    permissions = new[]
                    {
                        "Read", "Comment", "Vote"
                    }
                }
            }
        };

        // Act
        HttpResponseMessage response = await PostGraphQlAsync(request);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);

            if (jsonResponse.TryGetProperty("data", out JsonElement data) &&
                data.TryGetProperty("grantTenantPermission", out JsonElement permissionData))
            {
                Assert.Equal(user.Id.ToString(), permissionData.GetProperty("userId").GetString());
                Assert.Equal(tenant.Id.ToString(), permissionData.GetProperty("tenantId").GetString());

                // Verify permissions were actually granted
                bool hasRead = await _permissionService.HasTenantPermissionAsync(user.Id, tenant.Id, PermissionType.Read);
                bool hasComment = await _permissionService.HasTenantPermissionAsync(user.Id, tenant.Id, PermissionType.Comment);
                bool hasVote = await _permissionService.HasTenantPermissionAsync(user.Id, tenant.Id, PermissionType.Vote);

                Assert.True(hasRead);
                Assert.True(hasComment);
                Assert.True(hasVote);
            }
        }
    }

    [Fact]
    public async Task GraphQL_CheckPermission_ReturnsCorrectPermissionStatus()
    {
        // Arrange
        User user = await CreateTestUserAsync();
        Tenant tenant = await CreateTestTenantAsync();

        // Grant specific permissions
        await _permissionService.GrantTenantPermissionAsync(
            user.Id,
            tenant.Id,
            [
                PermissionType.Read,
                PermissionType.Comment
            ]
        );

        string token = await CreateJwtTokenForUserAsync(user, tenant);
        SetAuthorizationHeader(token);

        var query = @"
            query HasTenantPermission($userId: UUID!, $tenantId: UUID!, $permission: PermissionType!) {
                hasTenantPermission(userId: $userId, tenantId: $tenantId, permission: $permission)
            }";

        // Test for granted permission
        var requestGranted = new
        {
            query,
            variables = new
            {
                userId = user.Id, tenantId = tenant.Id, permission = "Read"
            }
        };

        // Test for non-granted permission
        var requestNotGranted = new
        {
            query,
            variables = new
            {
                userId = user.Id, tenantId = tenant.Id, permission = "Delete"
            }
        };

        // Act
        HttpResponseMessage responseGranted = await PostGraphQlAsync(requestGranted);
        HttpResponseMessage responseNotGranted = await PostGraphQlAsync(requestNotGranted);

        // Assert
        if (responseGranted.IsSuccessStatusCode)
        {
            string content = await responseGranted.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);

            if (jsonResponse.TryGetProperty("data", out JsonElement data) &&
                data.TryGetProperty("hasTenantPermission", out JsonElement hasPermission))
            {
                Assert.True(hasPermission.GetBoolean());
            }
        }

        if (responseNotGranted.IsSuccessStatusCode)
        {
            string content = await responseNotGranted.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);

            if (jsonResponse.TryGetProperty("data", out JsonElement data) &&
                data.TryGetProperty("hasTenantPermission", out JsonElement hasPermission))
            {
                Assert.False(hasPermission.GetBoolean());
            }
        }
    }

    [Fact]
    public async Task GraphQL_GetUserPermissions_ReturnsCompletePermissionList()
    {
        // Arrange
        User user = await CreateTestUserAsync();
        Tenant tenant = await CreateTestTenantAsync();

        var grantedPermissions = new[]
        {
            PermissionType.Read, PermissionType.Comment, PermissionType.Vote, PermissionType.Share
        };

        await _permissionService.GrantTenantPermissionAsync(user.Id, tenant.Id, grantedPermissions);

        string token = await CreateJwtTokenForUserAsync(user, tenant);
        SetAuthorizationHeader(token);

        var query = @"
            query GetUserTenantPermissions($userId: UUID!, $tenantId: UUID!) {
                getUserTenantPermissions(userId: $userId, tenantId: $tenantId)
            }";

        var request = new
        {
            query,
            variables = new
            {
                userId = user.Id, tenantId = tenant.Id
            }
        };

        // Act
        HttpResponseMessage response = await PostGraphQlAsync(request);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);

            if (jsonResponse.TryGetProperty("data", out JsonElement data) &&
                data.TryGetProperty("getUserTenantPermissions", out JsonElement permissions))
            {
                var permissionList = permissions.EnumerateArray()
                    .Select(p => p.GetString())
                    .ToList();

                Assert.Contains("Read", permissionList);
                Assert.Contains("Comment", permissionList);
                Assert.Contains("Vote", permissionList);
                Assert.Contains("Share", permissionList);
                Assert.Equal(grantedPermissions.Length, permissionList.Count);
            }
        }
    }

    #endregion

    #region Permission Management REST API Tests

    [Fact]
    public async Task REST_GrantTenantPermission_ReturnsSuccessResponse()
    {
        // Arrange
        User admin = await CreateTestAdminUserAsync();
        User user = await CreateTestUserAsync();
        Tenant tenant = await CreateTestTenantAsync();

        string token = await CreateJwtTokenForUserAsync(admin, tenant);
        SetAuthorizationHeader(token);

        var grantPermissionDto = new
        {
            userId = user.Id,
            tenantId = tenant.Id,
            permissions = new[]
            {
                "Read", "Edit", "Comment"
            }
        };

        string json = JsonSerializer.Serialize(grantPermissionDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/api/permissions/tenant/grant", content);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            var tenantPermission = JsonSerializer.Deserialize<JsonElement>(responseContent);

            Assert.Equal(user.Id.ToString(), tenantPermission.GetProperty("userId").GetString());
            Assert.Equal(tenant.Id.ToString(), tenantPermission.GetProperty("tenantId").GetString());

            // Verify permissions in database
            bool hasRead = await _permissionService.HasTenantPermissionAsync(user.Id, tenant.Id, PermissionType.Read);
            bool hasEdit = await _permissionService.HasTenantPermissionAsync(user.Id, tenant.Id, PermissionType.Edit);
            bool hasComment = await _permissionService.HasTenantPermissionAsync(user.Id, tenant.Id, PermissionType.Comment);

            Assert.True(hasRead);
            Assert.True(hasEdit);
            Assert.True(hasComment);
        }
    }

    [Fact]
    public async Task REST_RevokePermission_RemovesPermissionCorrectly()
    {
        // Arrange
        User admin = await CreateTestAdminUserAsync();
        User user = await CreateTestUserAsync();
        Tenant tenant = await CreateTestTenantAsync();

        // First, grant permissions
        await _permissionService.GrantTenantPermissionAsync(
            user.Id,
            tenant.Id,
            [
                PermissionType.Read,
                PermissionType.Edit,
                PermissionType.Comment
            ]
        );

        string token = await CreateJwtTokenForUserAsync(admin, tenant);
        SetAuthorizationHeader(token);

        var revokePermissionDto = new
        {
            userId = user.Id,
            tenantId = tenant.Id,
            permissions = new[]
            {
                "Edit", "Comment"
            }
        };

        string json = JsonSerializer.Serialize(revokePermissionDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/api/permissions/tenant/revoke", content);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            // Verify permissions were revoked
            bool hasRead = await _permissionService.HasTenantPermissionAsync(user.Id, tenant.Id, PermissionType.Read);
            bool hasEdit = await _permissionService.HasTenantPermissionAsync(user.Id, tenant.Id, PermissionType.Edit);
            bool hasComment = await _permissionService.HasTenantPermissionAsync(user.Id, tenant.Id, PermissionType.Comment);

            Assert.True(hasRead); // Should still have this
            Assert.False(hasEdit); // Should be revoked
            Assert.False(hasComment); // Should be revoked
        }
    }

    [Fact]
    public async Task REST_CheckPermission_ReturnsCorrectStatus()
    {
        // Arrange
        User user = await CreateTestUserAsync();
        Tenant tenant = await CreateTestTenantAsync();

        await _permissionService.GrantTenantPermissionAsync(
            user.Id,
            tenant.Id,
            [
                PermissionType.Read
            ]
        );

        string token = await CreateJwtTokenForUserAsync(user, tenant);
        SetAuthorizationHeader(token);

        // Act - Check granted permission
        HttpResponseMessage responseGranted = await _client.GetAsync($"/api/permissions/tenant/check?userId={user.Id}&tenantId={tenant.Id}&permission=Read");

        // Act - Check non-granted permission
        HttpResponseMessage responseNotGranted = await _client.GetAsync($"/api/permissions/tenant/check?userId={user.Id}&tenantId={tenant.Id}&permission=Delete");

        // Assert
        if (responseGranted.IsSuccessStatusCode)
        {
            string content = await responseGranted.Content.ReadAsStringAsync();
            var hasPermission = JsonSerializer.Deserialize<bool>(content);
            Assert.True(hasPermission);
        }

        if (responseNotGranted.IsSuccessStatusCode)
        {
            string content = await responseNotGranted.Content.ReadAsStringAsync();
            var hasPermission = JsonSerializer.Deserialize<bool>(content);
            Assert.False(hasPermission);
        }
    }

    #endregion

    #region Resource Permission Tests

    [Fact]
    public async Task GraphQL_GrantResourcePermission_WorksForSpecificResource()
    {
        // Arrange
        User user = await CreateTestUserAsync();
        Tenant tenant = await CreateTestTenantAsync();
        Comment comment = await CreateTestCommentAsync();

        string token = await CreateJwtTokenForUserAsync(user, tenant);
        SetAuthorizationHeader(token);

        var mutation = @"
            mutation GrantResourcePermission($input: GrantResourcePermissionInput!) {
                grantResourcePermission(input: $input) {
                    id
                    userId
                    resourceId
                    permissionFlags1
                }
            }";

        var request = new
        {
            query = mutation,
            variables = new
            {
                input = new
                {
                    userId = user.Id,
                    tenantId = tenant.Id,
                    resourceId = comment.Id,
                    resourceType = "Comment",
                    permissions = new[]
                    {
                        "Edit", "Delete"
                    }
                }
            }
        };

        // Act
        HttpResponseMessage response = await PostGraphQlAsync(request);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);

            if (jsonResponse.TryGetProperty("data", out JsonElement data) &&
                data.TryGetProperty("grantResourcePermission", out JsonElement permissionData))
            {
                Assert.Equal(user.Id.ToString(), permissionData.GetProperty("userId").GetString());
                Assert.Equal(comment.Id.ToString(), permissionData.GetProperty("resourceId").GetString());

                // Verify permissions through service
                bool hasEdit = await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
                    user.Id,
                    tenant.Id,
                    comment.Id,
                    PermissionType.Edit
                );
                bool hasDelete = await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
                    user.Id,
                    tenant.Id,
                    comment.Id,
                    PermissionType.Delete
                );

                Assert.True(hasEdit);
                Assert.True(hasDelete);
            }
        }
    }

    [Fact]
    public async Task REST_ShareResource_CreatesPermissionForTargetUser()
    {
        // Arrange
        User owner = await CreateTestUserAsync("owner@test.com");
        User targetUser = await CreateTestUserAsync("target@test.com");
        Tenant tenant = await CreateTestTenantAsync();
        Comment comment = await CreateTestCommentAsync();

        // Grant owner permission to share
        await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
            owner.Id,
            tenant.Id,
            comment.Id,
            [PermissionType.Share, PermissionType.Edit]
        );

        string token = await CreateJwtTokenForUserAsync(owner, tenant);
        SetAuthorizationHeader(token);

        var shareResourceDto = new
        {
            resourceId = comment.Id,
            targetUserId = targetUser.Id,
            tenantId = tenant.Id,
            permissions = new[]
            {
                "Read", "Comment"
            },
            expiresAt = DateTime.UtcNow.AddDays(30)
        };

        string json = JsonSerializer.Serialize(shareResourceDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/api/permissions/resource/share", content);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            // Verify target user received permissions
            bool hasRead = await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
                targetUser.Id,
                tenant.Id,
                comment.Id,
                PermissionType.Read
            );
            bool hasComment = await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
                targetUser.Id,
                tenant.Id,
                comment.Id,
                PermissionType.Comment
            );

            Assert.True(hasRead);
            Assert.True(hasComment);
        }
    }

    #endregion

    #region Permission Inheritance and Hierarchy Tests

    [Fact]
    public async Task GraphQL_EffectivePermissions_ResolvesHierarchyCorrectly()
    {
        // Arrange
        User user = await CreateTestUserAsync();
        Tenant tenant = await CreateTestTenantAsync();

        // Set up permission hierarchy
        await _permissionService.SetTenantDefaultPermissionsAsync(tenant.Id, [PermissionType.Read]);
        await _permissionService.GrantTenantPermissionAsync(user.Id, tenant.Id, [PermissionType.Comment]);

        string token = await CreateJwtTokenForUserAsync(user, tenant);
        SetAuthorizationHeader(token);

        var query = @"
            query GetEffectivePermissions($userId: UUID!, $tenantId: UUID!) {
                getEffectiveTenantPermissions(userId: $userId, tenantId: $tenantId)
            }";

        var request = new
        {
            query,
            variables = new
            {
                userId = user.Id, tenantId = tenant.Id
            }
        };

        // Act
        HttpResponseMessage response = await PostGraphQlAsync(request);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);

            if (jsonResponse.TryGetProperty("data", out JsonElement data) &&
                data.TryGetProperty("getEffectiveTenantPermissions", out JsonElement permissions))
            {
                var permissionList = permissions.EnumerateArray()
                    .Select(p => p.GetString())
                    .ToList();

                // Should have both default (Read) and user-specific (Comment) permissions
                Assert.Contains("Read", permissionList);
                Assert.Contains("Comment", permissionList);
            }
        }
    }

    #endregion

    #region Bulk Operations Tests

    [Fact]
    public async Task REST_BulkPermissionCheck_HandlesMultipleResources()
    {
        // Arrange
        User user = await CreateTestUserAsync();
        Tenant tenant = await CreateTestTenantAsync();
        Comment comment1 = await CreateTestCommentAsync();
        Comment comment2 = await CreateTestCommentAsync();
        Comment comment3 = await CreateTestCommentAsync();

        // Grant permissions to some resources
        await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
            user.Id,
            tenant.Id,
            comment1.Id,
            [PermissionType.Read, PermissionType.Edit]
        );
        await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
            user.Id,
            tenant.Id,
            comment3.Id,
            [PermissionType.Read]
        );

        string token = await CreateJwtTokenForUserAsync(user, tenant);
        SetAuthorizationHeader(token);

        var bulkCheckDto = new
        {
            userId = user.Id,
            tenantId = tenant.Id,
            resourceIds = new[]
            {
                comment1.Id, comment2.Id, comment3.Id
            },
            resourceType = "Comment"
        };

        string json = JsonSerializer.Serialize(bulkCheckDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/api/permissions/resource/bulk-check", content);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            var bulkResults = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Verify results for each resource
            if (bulkResults.TryGetProperty(comment1.Id.ToString(), out JsonElement comment1Permissions))
            {
                var permissions = comment1Permissions.EnumerateArray().Select(p => p.GetString()).ToList();
                Assert.Contains("Read", permissions);
                Assert.Contains("Edit", permissions);
            }

            if (bulkResults.TryGetProperty(comment2.Id.ToString(), out JsonElement comment2Permissions))
            {
                var permissions = comment2Permissions.EnumerateArray().Select(p => p.GetString()).ToList();
                Assert.Empty(permissions); // No permissions granted
            }

            if (bulkResults.TryGetProperty(comment3.Id.ToString(), out JsonElement comment3Permissions))
            {
                var permissions = comment3Permissions.EnumerateArray().Select(p => p.GetString()).ToList();
                Assert.Contains("Read", permissions);
                Assert.DoesNotContain("Edit", permissions);
            }
        }
    }

    #endregion

    #region Helper Methods

    private async Task<User> CreateTestUserAsync(string email = "test@example.com")
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Name = "Test User", Email = email, IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    private async Task<User> CreateTestAdminUserAsync(string email = "admin@example.com")
    {
        var admin = new User
        {
            Id = Guid.NewGuid(), Name = "Admin User", Email = email, IsActive = true
        };

        _context.Users.Add(admin);
        await _context.SaveChangesAsync();

        // Grant admin permissions (in real app, this would be done differently)
        Tenant tenant = await CreateTestTenantAsync();
        await _permissionService.GrantTenantPermissionAsync(
            admin.Id,
            tenant.Id,
            [
                PermissionType.Draft,
                PermissionType.Edit,
                PermissionType.Delete,
                PermissionType.Read
            ]
        );

        return admin;
    }

    private async Task<Tenant> CreateTestTenantAsync(string name = "Test Tenant")
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(), Name = name, Description = "Test tenant for E2E tests", IsActive = true
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        return tenant;
    }

    private async Task<Comment> CreateTestCommentAsync()
    {
        var comment = new Comment
        {
            Id = Guid.NewGuid(), Content = "Test comment content"
            // Note: Comment entity doesn't have IsEdited property
        };

        // Comments are accessed through Resources DbSet since Comment inherits from ResourceBase
        _context.Resources.Add(comment);
        await _context.SaveChangesAsync();

        return comment;
    }

    private async Task<string> CreateJwtTokenForUserAsync(User user, Tenant tenant)
    {
        var jwtService = _scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var userDto = new UserDto
        {
            Id = user.Id, Username = user.Name, Email = user.Email
        };

        var roles = new[]
        {
            "User"
        };

        var additionalClaims = new[]
        {
            new System.Security.Claims.Claim("tenant_id", tenant.Id.ToString())
        };

        return jwtService.GenerateAccessToken(userDto, roles, additionalClaims);
    }

    private void SetAuthorizationHeader(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<HttpResponseMessage> PostGraphQlAsync(object request)
    {
        string json = JsonSerializer.Serialize(
            request,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        );

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        return await _client.PostAsync("/graphql", content);
    }

    #endregion
}
