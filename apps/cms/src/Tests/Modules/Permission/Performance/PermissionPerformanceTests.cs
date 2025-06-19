using Xunit;
using Microsoft.EntityFrameworkCore;
using GameGuild.Data;
using GameGuild.Common.Services;
using GameGuild.Common.Entities;
using GameGuild.Modules.Tenant.Models;
using GameGuild.Modules.Comment.Models;
using GameGuild.Modules.User.Models;
using System.Diagnostics;

namespace GameGuild.Tests.Modules.Permission.Performance;

/// <summary>
/// Performance tests for Permission Module
/// Tests scalability and performance under load
/// </summary>
public class PermissionPerformanceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PermissionService _permissionService;

    public PermissionPerformanceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new ApplicationDbContext(options);
        _permissionService = new PermissionService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Bulk Permission Operations Performance Tests

    [Fact]
    public async Task BulkPermissionGrant_HandlesLargeUserCount_WithinReasonableTime()
    {
        // Arrange
        const int userCount = 1000;
        const int maxAcceptableTimeMs = 5000; // 5 seconds
        
        var tenant = await CreateTestTenantAsync();
        var users = new List<User>();
        
        // Create test users
        for (int i = 0; i < userCount; i++)
        {
            users.Add(await CreateTestUserAsync($"user{i}@test.com"));
        }

        var permissions = new[] { PermissionType.Read, PermissionType.Comment, PermissionType.Vote };
        var stopwatch = Stopwatch.StartNew();

        // Act - Grant permissions to all users
        var tasks = users.Select(async user => 
            await _permissionService.GrantTenantPermissionAsync(user.Id, tenant.Id, permissions)
        );
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < maxAcceptableTimeMs, 
            $"Bulk permission grant took {stopwatch.ElapsedMilliseconds}ms, expected under {maxAcceptableTimeMs}ms");
        
        // Verify a sample of permissions were actually granted
        var sampleUser = users[userCount / 2];
        var hasRead = await _permissionService.HasTenantPermissionAsync(sampleUser.Id, tenant.Id, PermissionType.Read);
        var hasComment = await _permissionService.HasTenantPermissionAsync(sampleUser.Id, tenant.Id, PermissionType.Comment);
        
        Assert.True(hasRead);
        Assert.True(hasComment);
    }

    [Fact]
    public async Task BulkPermissionCheck_HandlesLargeUserCount_WithinReasonableTime()
    {
        // Arrange
        const int userCount = 500;
        const int maxAcceptableTimeMs = 3000; // 3 seconds
        
        var tenant = await CreateTestTenantAsync();
        var users = new List<User>();
        
        // Create and grant permissions to test users
        for (int i = 0; i < userCount; i++)
        {
            var user = await CreateTestUserAsync($"user{i}@test.com");
            users.Add(user);
            
            await _permissionService.GrantTenantPermissionAsync(user.Id, tenant.Id, 
                new[] { PermissionType.Read, PermissionType.Comment });
        }

        var stopwatch = Stopwatch.StartNew();

        // Act - Check permissions for all users
        var checkTasks = users.Select(async user => 
            await _permissionService.HasTenantPermissionAsync(user.Id, tenant.Id, PermissionType.Read)
        );
        
        var results = await Task.WhenAll(checkTasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < maxAcceptableTimeMs, 
            $"Bulk permission check took {stopwatch.ElapsedMilliseconds}ms, expected under {maxAcceptableTimeMs}ms");
        
        Assert.True(results.All(r => r), "All users should have Read permission");
    }

    [Fact]
    public async Task BulkResourcePermissions_HandlesLargeResourceCount_WithinReasonableTime()
    {
        // Arrange
        const int resourceCount = 1000;
        const int maxAcceptableTimeMs = 8000; // 8 seconds
        
        var user = await CreateTestUserAsync();
        var tenant = await CreateTestTenantAsync();
        var resources = new List<Comment>();
        
        // Create test resources
        for (int i = 0; i < resourceCount; i++)
        {
            resources.Add(await CreateTestCommentAsync($"Comment content {i}"));
        }

        var permissions = new[] { PermissionType.Read, PermissionType.Edit };
        var stopwatch = Stopwatch.StartNew();

        // Act - Grant resource permissions
        var grantTasks = resources.Select(async resource => 
            await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
                user.Id, tenant.Id, resource.Id, permissions)
        );
        
        await Task.WhenAll(grantTasks);
        
        // Now check bulk permissions
        var resourceIds = resources.Select(r => r.Id).ToArray();
        var bulkResult = await _permissionService.GetBulkResourcePermissionsAsync<CommentPermission, Comment>(
            user.Id, tenant.Id, resourceIds);
        
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < maxAcceptableTimeMs, 
            $"Bulk resource operations took {stopwatch.ElapsedMilliseconds}ms, expected under {maxAcceptableTimeMs}ms");
        
        Assert.Equal(resourceCount, bulkResult.Count);
        
        // Verify sample permissions
        var sampleResource = resources[resourceCount / 2];
        Assert.True(bulkResult.ContainsKey(sampleResource.Id));
        Assert.Contains(PermissionType.Read, bulkResult[sampleResource.Id]);
        Assert.Contains(PermissionType.Edit, bulkResult[sampleResource.Id]);
    }

    #endregion

    #region Memory Usage Tests

    [Fact]
    public async Task PermissionHierarchyResolution_DoesNotCauseMemoryLeaks()
    {
        // Arrange
        const int iterationCount = 100;
        var user = await CreateTestUserAsync();
        var tenant = await CreateTestTenantAsync();
        
        // Set up complex permission hierarchy
        await _permissionService.SetGlobalDefaultPermissionsAsync(new[] { PermissionType.Read });
        await _permissionService.SetTenantDefaultPermissionsAsync(tenant.Id, new[] { PermissionType.Comment });
        await _permissionService.GrantTenantPermissionAsync(user.Id, tenant.Id, new[] { PermissionType.Vote });

        var initialMemory = GC.GetTotalMemory(true);

        // Act - Repeatedly resolve effective permissions
        for (int i = 0; i < iterationCount; i++)
        {
            var effectivePermissions = await _permissionService.GetEffectiveTenantPermissionsAsync(user.Id, tenant.Id);
            var permissionList = effectivePermissions.ToList(); // Force enumeration
            
            Assert.NotEmpty(permissionList);
            
            // Force garbage collection every 10 iterations
            if (i % 10 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert - Memory increase should be reasonable (less than 10MB)
        Assert.True(memoryIncrease < 10 * 1024 * 1024, 
            $"Memory increased by {memoryIncrease} bytes, which may indicate a memory leak");
    }

    #endregion

    #region Database Query Performance Tests

    [Fact]
    public async Task ComplexPermissionQuery_ExecutesWithinReasonableTime()
    {
        // Arrange
        const int maxAcceptableTimeMs = 1000; // 1 second
        
        var user = await CreateTestUserAsync();
        var tenant = await CreateTestTenantAsync();
        var comment = await CreateTestCommentAsync();
        
        // Create complex permission scenario
        await _permissionService.SetGlobalDefaultPermissionsAsync(new[] { PermissionType.Read });
        await _permissionService.SetTenantDefaultPermissionsAsync(tenant.Id, new[] { PermissionType.Comment });
        await _permissionService.GrantTenantPermissionAsync(user.Id, tenant.Id, new[] { PermissionType.Vote });
        await _permissionService.GrantContentTypePermissionAsync(user.Id, tenant.Id, "Comment", new[] { PermissionType.Reply });
        await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
            user.Id, tenant.Id, comment.Id, new[] { PermissionType.Edit });

        var stopwatch = Stopwatch.StartNew();

        // Act - Perform complex permission checks
        var hasRead = await _permissionService.HasTenantPermissionAsync(user.Id, tenant.Id, PermissionType.Read);
        var hasComment = await _permissionService.HasTenantPermissionAsync(user.Id, tenant.Id, PermissionType.Comment);
        var hasVote = await _permissionService.HasTenantPermissionAsync(user.Id, tenant.Id, PermissionType.Vote);
        var hasReply = await _permissionService.HasContentTypePermissionAsync(user.Id, tenant.Id, "Comment", PermissionType.Reply);
        var hasEdit = await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
            user.Id, tenant.Id, comment.Id, PermissionType.Edit);
        var effectivePermissions = await _permissionService.GetEffectiveTenantPermissionsAsync(user.Id, tenant.Id);
        var effectiveList = effectivePermissions.ToList();

        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < maxAcceptableTimeMs, 
            $"Complex permission query took {stopwatch.ElapsedMilliseconds}ms, expected under {maxAcceptableTimeMs}ms");
        
        Assert.True(hasRead);
        Assert.True(hasComment);
        Assert.True(hasVote);
        Assert.True(hasReply);
        Assert.True(hasEdit);
        Assert.Contains(PermissionType.Read, effectiveList);
        Assert.Contains(PermissionType.Comment, effectiveList);
        Assert.Contains(PermissionType.Vote, effectiveList);
    }

    [Fact]
    public async Task TenantMembershipQueries_ScaleWithUserCount()
    {
        // Arrange
        const int userCount = 200;
        const int tenantCount = 10;
        const int maxAcceptableTimeMs = 3000; // 3 seconds
        
        var tenants = new List<Tenant>();
        var users = new List<User>();
        
        // Create tenants
        for (int i = 0; i < tenantCount; i++)
        {
            tenants.Add(await CreateTestTenantAsync($"Tenant {i}"));
        }
        
        // Create users and assign them to random tenants
        var random = new Random(42); // Fixed seed for reproducibility
        for (int i = 0; i < userCount; i++)
        {
            var user = await CreateTestUserAsync($"user{i}@test.com");
            users.Add(user);
            
            // Assign user to 2-3 random tenants
            var tenantAssignments = random.Next(2, 4);
            var selectedTenants = tenants.OrderBy(_ => random.Next()).Take(tenantAssignments);
            
            foreach (var tenant in selectedTenants)
            {
                await _permissionService.JoinTenantAsync(user.Id, tenant.Id);
            }
        }

        var stopwatch = Stopwatch.StartNew();

        // Act - Query tenant memberships for all users
        var membershipTasks = users.Select(async user => 
        {
            var userTenants = await _permissionService.GetUserTenantsAsync(user.Id);
            return userTenants.Count();
        });
        
        var membershipCounts = await Task.WhenAll(membershipTasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < maxAcceptableTimeMs, 
            $"Tenant membership queries took {stopwatch.ElapsedMilliseconds}ms, expected under {maxAcceptableTimeMs}ms");
        
        Assert.True(membershipCounts.All(count => count >= 2 && count <= 3));
        Assert.Equal(userCount, membershipCounts.Length);
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task ConcurrentPermissionOperations_HandleMultipleUsers()
    {
        // Arrange
        const int concurrentUsers = 50;
        const int operationsPerUser = 10;
        
        var tenant = await CreateTestTenantAsync();
        var users = new List<User>();
        
        for (int i = 0; i < concurrentUsers; i++)
        {
            users.Add(await CreateTestUserAsync($"concurrent_user{i}@test.com"));
        }

        var stopwatch = Stopwatch.StartNew();

        // Act - Perform concurrent permission operations
        var concurrentTasks = users.Select(async user =>
        {
            var userTasks = new List<Task>();
            
            for (int i = 0; i < operationsPerUser; i++)
            {
                userTasks.Add(_permissionService.GrantTenantPermissionAsync(
                    user.Id, tenant.Id, new[] { PermissionType.Read, PermissionType.Comment }));
                
                userTasks.Add(Task.Run(async () =>
                {
                    await _permissionService.HasTenantPermissionAsync(user.Id, tenant.Id, PermissionType.Read);
                }));
            }
            
            await Task.WhenAll(userTasks);
        });
        
        await Task.WhenAll(concurrentTasks);
        stopwatch.Stop();

        // Assert - Should complete within reasonable time without deadlocks
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, // 10 seconds
            $"Concurrent operations took {stopwatch.ElapsedMilliseconds}ms, may indicate deadlock issues");
        
        // Verify all users have permissions
        var verificationTasks = users.Select(async user => 
            await _permissionService.HasTenantPermissionAsync(user.Id, tenant.Id, PermissionType.Read));
        
        var results = await Task.WhenAll(verificationTasks);
        Assert.True(results.All(r => r), "All users should have Read permission after concurrent operations");
    }

    #endregion

    #region Helper Methods

    private async Task<User> CreateTestUserAsync(string email = "test@example.com")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = $"Test User {email}",
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
            Description = $"Test tenant: {name}",
            IsActive = true
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        return tenant;
    }

    private async Task<Comment> CreateTestCommentAsync(string content = "Test comment content")
    {
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Content = content
            // Note: Comment entity doesn't have IsEdited property
        };

        // Comments are accessed through Resources DbSet since Comment inherits from ResourceBase
        _context.Resources.Add(comment);
        await _context.SaveChangesAsync();
        return comment;
    }

    #endregion
}
