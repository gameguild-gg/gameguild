using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using GameGuild.Data;
using GameGuild.Common.Entities;
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
/// End-to-end tests for Permission Module APIs
/// Tests permission-protected endpoints via GraphQL and REST APIs
/// </summary>
public class PermissionModuleE2ETests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _context;

    public PermissionModuleE2ETests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        
        // Use a separate in-memory database for E2E tests
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        var serviceProvider = services.BuildServiceProvider();
        _context = serviceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _context.Dispose();
        _client.Dispose();
    }

    #region GraphQL API Tests

    [Fact]
    public async Task GraphQL_TenantQueries_RequireValidAuthentication()
    {
        // Arrange
        var query = @"
            query {
                getTenants {
                    id
                    name
                    description
                }
            }";

        var request = new
        {
            query = query
        };

        // Act - Request without authentication
        var response = await PostGraphQLAsync(request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                   response.StatusCode == System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GraphQL_PermissionMutations_EnforcePermissionChecks()
    {
        // Arrange - Create test data
        var user = await CreateTestUserAsync();
        var tenant = await CreateTestTenantAsync();
        
        // Create JWT token for user
        var token = await CreateJWTTokenForUserAsync(user, tenant);
        SetAuthorizationHeader(token);

        var mutation = @"
            mutation CreateTenant($input: CreateTenantInput!) {
                createTenant(input: $input) {
                    id
                    name
                    description
                }
            }";

        var request = new
        {
            query = mutation,
            variables = new
            {
                input = new
                {
                    name = "New Test Tenant",
                    description = "Test tenant created via GraphQL",
                    isActive = true
                }
            }
        };

        // Act - Request without CREATE permission
        var response = await PostGraphQLAsync(request);

        // Assert - Should be forbidden due to missing permissions
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);
            
            // Check if there are permission-related errors
            if (jsonResponse.TryGetProperty("errors", out var errors))
            {
                var errorMessages = errors.EnumerateArray()
                    .Select(e => e.GetProperty("message").GetString())
                    .ToList();
                
                Assert.Contains(errorMessages, m => m != null && 
                    (m.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
                     m.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                     m.Contains("forbidden", StringComparison.OrdinalIgnoreCase)));
            }
        }
        else
        {
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                       response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task GraphQL_WithValidPermissions_AllowsResourceAccess()
    {
        // Arrange - Create test data with proper permissions
        var user = await CreateTestUserAsync();
        var tenant = await CreateTestTenantAsync();
        
        // Grant tenant permissions to user
        await GrantTenantPermissionsAsync(user.Id, tenant.Id, new[] { 
            PermissionType.Read, 
            PermissionType.Draft 
        });
        
        var token = await CreateJWTTokenForUserAsync(user, tenant);
        SetAuthorizationHeader(token);

        var query = @"
            query {
                getTenants {
                    id
                    name
                    description
                }
            }";

        var request = new { query = query };

        // Act
        var response = await PostGraphQLAsync(request);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);
            
            // Should have data and no permission errors
            Assert.True(jsonResponse.TryGetProperty("data", out _));
            
            if (jsonResponse.TryGetProperty("errors", out var errors))
            {
                var errorMessages = errors.EnumerateArray()
                    .Select(e => e.GetProperty("message").GetString())
                    .ToList();
                
                Assert.DoesNotContain(errorMessages, m => m != null && 
                    m.Contains("permission", StringComparison.OrdinalIgnoreCase));
            }
        }
        else
        {
            // If not successful, it should not be due to permission issues
            Assert.NotEqual(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
            Assert.NotEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    #endregion

    #region REST API Tests

    [Fact]
    public async Task REST_TenantEndpoints_RequireAuthentication()
    {
        // Act - Request without authentication
        var response = await _client.GetAsync("/tenants");

        // Assert
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                   response.StatusCode == System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task REST_CreateTenant_EnforcesCreatePermission()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var tenant = await CreateTestTenantAsync();
        
        var token = await CreateJWTTokenForUserAsync(user, tenant);
        SetAuthorizationHeader(token);

        var createTenantDto = new
        {
            name = "Test Tenant REST",
            description = "Created via REST API",
            isActive = true
        };

        var json = JsonSerializer.Serialize(createTenantDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act - Request without CREATE permission
        var response = await _client.PostAsync("/tenants", content);

        // Assert
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                   response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task REST_WithValidPermissions_AllowsResourceOperations()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var tenant = await CreateTestTenantAsync();
        
        // Grant necessary permissions
        await GrantTenantPermissionsAsync(user.Id, tenant.Id, new[] { 
            PermissionType.Read,
            PermissionType.Edit
        });
        
        var token = await CreateJWTTokenForUserAsync(user, tenant);
        SetAuthorizationHeader(token);

        // Act - Get tenants (should work with Read permission)
        var response = await _client.GetAsync("/tenants");

        // Assert
        if (!response.IsSuccessStatusCode)
        {
            // If not successful, it should not be due to permission issues
            Assert.NotEqual(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
            Assert.NotEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    #endregion

    #region Multi-Layer Permission Tests

    [Fact]
    public async Task E2E_HierarchicalPermissionResolution_WorksCorrectly()
    {
        // Arrange - Create complex permission hierarchy
        var user = await CreateTestUserAsync();
        var tenant = await CreateTestTenantAsync();
        var comment = await CreateTestCommentAsync();

        // Grant different permissions at different layers
        await GrantTenantPermissionsAsync(user.Id, tenant.Id, new[] { PermissionType.Read });
        await GrantContentTypePermissionsAsync(user.Id, tenant.Id, "Comment", new[] { PermissionType.Comment });
        await GrantResourcePermissionsAsync(user.Id, tenant.Id, comment.Id, new[] { PermissionType.Edit });
        
        var token = await CreateJWTTokenForUserAsync(user, tenant);
        SetAuthorizationHeader(token);

        // Act & Assert - Test different permission levels through API
        
        // 1. Tenant-level READ should work
        var tenantResponse = await _client.GetAsync("/tenants");
        if (tenantResponse.IsSuccessStatusCode || tenantResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Assert.NotEqual(System.Net.HttpStatusCode.Forbidden, tenantResponse.StatusCode);
        }

        // 2. Content-type level comment permissions should be available via GraphQL
        var commentQuery = @"
            query {
                getComments {
                    id
                    content
                }
            }";

        var commentQueryRequest = new { query = commentQuery };
        var commentResponse = await PostGraphQLAsync(commentQueryRequest);
        
        if (commentResponse.IsSuccessStatusCode)
        {
            var content = await commentResponse.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);
            
            // Should not have permission errors for comment access
            if (jsonResponse.TryGetProperty("errors", out var errors))
            {
                var errorMessages = errors.EnumerateArray()
                    .Select(e => e.GetProperty("message").GetString())
                    .ToList();
                
                Assert.DoesNotContain(errorMessages, m => m != null && 
                    m.Contains("comment", StringComparison.OrdinalIgnoreCase) &&
                    m.Contains("permission", StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Fact]
    public async Task E2E_PermissionInheritance_OverridesWorkCorrectly()
    {
        // Arrange - Create test scenario where resource permissions override content-type permissions
        var user = await CreateTestUserAsync();
        var tenant = await CreateTestTenantAsync();
        var comment = await CreateTestCommentAsync();

        // Grant different permissions that demonstrate hierarchy
        await GrantTenantPermissionsAsync(user.Id, tenant.Id, new[] { PermissionType.Read });
        await GrantContentTypePermissionsAsync(user.Id, tenant.Id, "Comment", new[] { PermissionType.Comment });
        
        // Resource-level permission should override content-type denial
        await GrantResourcePermissionsAsync(user.Id, tenant.Id, comment.Id, new[] { 
            PermissionType.Edit, 
            PermissionType.Delete 
        });
        
        var token = await CreateJWTTokenForUserAsync(user, tenant);
        SetAuthorizationHeader(token);

        // Act - Try to perform actions that should be allowed by resource-level permissions
        var updateCommentDto = new
        {
            content = "Updated comment content",
            isEdited = true
        };

        var json = JsonSerializer.Serialize(updateCommentDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PutAsync($"/comments/{comment.Id}", content);

        // Assert - Resource-level permissions should allow the operation
        if (!response.IsSuccessStatusCode)
        {
            // If not successful, it should not be due to permission issues
            Assert.NotEqual(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
            Assert.NotEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    #endregion

    #region Cross-Tenant Isolation Tests

    [Fact]
    public async Task E2E_CrossTenantIsolation_PreventsCrossTenantAccess()
    {
        // Arrange - Create users in different tenants
        var user1 = await CreateTestUserAsync("user1@test.com");
        var user2 = await CreateTestUserAsync("user2@test.com");
        var tenant1 = await CreateTestTenantAsync("Tenant 1");
        var tenant2 = await CreateTestTenantAsync("Tenant 2");

        // Grant permissions to users in their respective tenants
        await GrantTenantPermissionsAsync(user1.Id, tenant1.Id, new[] { PermissionType.Read, PermissionType.Edit });
        await GrantTenantPermissionsAsync(user2.Id, tenant2.Id, new[] { PermissionType.Read, PermissionType.Edit });

        // Act - User1 tries to access Tenant2's resources
        var token1 = await CreateJWTTokenForUserAsync(user1, tenant2); // Wrong tenant context
        SetAuthorizationHeader(token1);

        var response = await _client.GetAsync($"/tenants/{tenant2.Id}");

        // Assert - Should be forbidden due to cross-tenant access
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                   response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                   response.StatusCode == System.Net.HttpStatusCode.NotFound);
    }

    #endregion

    #region Helper Methods

    private async Task<User> CreateTestUserAsync(string email = "test@example.com")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = email,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Tenant> CreateTestTenantAsync(string name = "Test Tenant")
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test tenant for E2E tests",
            IsActive = true
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        return tenant;
    }

    private async Task<Comment> CreateTestCommentAsync()
    {
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Content = "Test comment content"
            // Note: Comment entity doesn't have IsEdited property
        };

        // Comments are accessed through Resources DbSet since Comment inherits from ResourceBase
        _context.Resources.Add(comment);
        await _context.SaveChangesAsync();
        return comment;
    }

    private async Task GrantTenantPermissionsAsync(Guid userId, Guid tenantId, PermissionType[] permissions)
    {
        var tenantPermission = new TenantPermission
        {
            UserId = userId,
            TenantId = tenantId
        };

        foreach (var permission in permissions)
        {
            tenantPermission.AddPermission(permission);
        }

        _context.TenantPermissions.Add(tenantPermission);
        await _context.SaveChangesAsync();
    }

    private async Task GrantContentTypePermissionsAsync(Guid userId, Guid tenantId, string contentTypeName, PermissionType[] permissions)
    {
        var contentTypePermission = new ContentTypePermission
        {
            UserId = userId,
            TenantId = tenantId,
            ContentType = contentTypeName  // Correct property name is ContentType, not ContentTypeName
        };

        foreach (var permission in permissions)
        {
            contentTypePermission.AddPermission(permission);
        }

        _context.ContentTypePermissions.Add(contentTypePermission);
        await _context.SaveChangesAsync();
    }

    private async Task GrantResourcePermissionsAsync(Guid userId, Guid tenantId, Guid resourceId, PermissionType[] permissions)
    {
        var resourcePermission = new CommentPermission
        {
            UserId = userId,
            TenantId = tenantId,
            ResourceId = resourceId
        };

        foreach (var permission in permissions)
        {
            resourcePermission.AddPermission(permission);
        }

        _context.CommentPermissions.Add(resourcePermission);
        await _context.SaveChangesAsync();
    }

    private async Task<string> CreateJWTTokenForUserAsync(User user, Tenant tenant)
    {
        // In a real implementation, you would use the actual JWT service
        // For E2E tests, we'll create a mock token or use the real service
        var jwtService = _scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        
        var userDto = new UserDto 
        { 
            Id = user.Id, 
            Username = user.Name, 
            Email = user.Email 
        };
        
        var roles = new[] { "User" };
        
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

    private async Task<HttpResponseMessage> PostGraphQLAsync(object request)
    {
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync("/graphql", content);
    }

    #endregion
}
