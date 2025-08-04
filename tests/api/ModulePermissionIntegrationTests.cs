using GameGuild.Common.Services;
using GameGuild.Modules.Permissions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Tests.ModulePermissions;

/// <summary>
/// Integration tests for the Module Permission System
/// Tests the complete flow from role assignment to permission checking
/// </summary>
public class ModulePermissionIntegrationTests
{
    private readonly IModulePermissionService _modulePermissionService;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testTenantId = Guid.NewGuid();

    public ModulePermissionIntegrationTests()
    {
        var logger = new NullLogger<ModulePermissionService>();
        _modulePermissionService = new ModulePermissionService(logger);
    }

    [Fact]
    public async Task AssignTestingLabAdminRole_ShouldGrantAllPermissions()
    {
        // Arrange & Act
        await _modulePermissionService.AssignRoleAsync(
            _testUserId, 
            _testTenantId, 
            ModuleType.TestingLab, 
            "TestingLabAdmin");

        // Assert - Check all admin permissions
        Assert.True(await _modulePermissionService.CanCreateTestingSessionsAsync(_testUserId, _testTenantId));
        Assert.True(await _modulePermissionService.CanDeleteTestingSessionsAsync(_testUserId, _testTenantId));
        Assert.True(await _modulePermissionService.CanManageTestersAsync(_testUserId, _testTenantId));
        Assert.True(await _modulePermissionService.CanViewTestingReportsAsync(_testUserId, _testTenantId));
        Assert.True(await _modulePermissionService.CanExportTestingDataAsync(_testUserId, _testTenantId));
        
        // Check comprehensive permissions
        var permissions = await _modulePermissionService.GetUserTestingLabPermissionsAsync(_testUserId, _testTenantId);
        Assert.True(permissions.CanCreateSessions);
        Assert.True(permissions.CanDeleteSessions);
        Assert.True(permissions.CanManageTesters);
        Assert.True(permissions.CanViewReports);
        Assert.True(permissions.CanExportData);
        Assert.True(permissions.CanAdminister);
        Assert.Contains("TestingLabAdmin", permissions.AssignedRoles);
    }

    [Fact]
    public async Task AssignTestingLabManagerRole_ShouldGrantLimitedPermissions()
    {
        // Arrange & Act
        await _modulePermissionService.AssignRoleAsync(
            _testUserId, 
            _testTenantId, 
            ModuleType.TestingLab, 
            "TestingLabManager");

        // Assert - Manager can create/edit but not delete
        Assert.True(await _modulePermissionService.CanCreateTestingSessionsAsync(_testUserId, _testTenantId));
        Assert.False(await _modulePermissionService.CanDeleteTestingSessionsAsync(_testUserId, _testTenantId));
        Assert.True(await _modulePermissionService.CanManageTestersAsync(_testUserId, _testTenantId));
        Assert.True(await _modulePermissionService.CanViewTestingReportsAsync(_testUserId, _testTenantId));
        Assert.False(await _modulePermissionService.CanExportTestingDataAsync(_testUserId, _testTenantId));
        
        // Check comprehensive permissions
        var permissions = await _modulePermissionService.GetUserTestingLabPermissionsAsync(_testUserId, _testTenantId);
        Assert.True(permissions.CanCreateSessions);
        Assert.True(permissions.CanEditSessions);
        Assert.False(permissions.CanDeleteSessions);
        Assert.True(permissions.CanManageTesters);
        Assert.True(permissions.CanViewReports);
        Assert.False(permissions.CanExportData);
        Assert.False(permissions.CanAdminister);
        Assert.Contains("TestingLabManager", permissions.AssignedRoles);
    }

    [Fact]
    public async Task AssignTestingLabTesterRole_ShouldGrantMinimalPermissions()
    {
        // Arrange & Act
        await _modulePermissionService.AssignRoleAsync(
            _testUserId, 
            _testTenantId, 
            ModuleType.TestingLab, 
            "TestingLabTester");

        // Assert - Tester can only read
        Assert.False(await _modulePermissionService.CanCreateTestingSessionsAsync(_testUserId, _testTenantId));
        Assert.False(await _modulePermissionService.CanDeleteTestingSessionsAsync(_testUserId, _testTenantId));
        Assert.False(await _modulePermissionService.CanManageTestersAsync(_testUserId, _testTenantId));
        Assert.False(await _modulePermissionService.CanViewTestingReportsAsync(_testUserId, _testTenantId));
        Assert.False(await _modulePermissionService.CanExportTestingDataAsync(_testUserId, _testTenantId));
        
        // But should have read access through HasModulePermissionAsync
        Assert.True(await _modulePermissionService.HasModulePermissionAsync(_testUserId, _testTenantId, ModuleType.TestingLab, ModuleAction.Read));
        
        // Check comprehensive permissions
        var permissions = await _modulePermissionService.GetUserTestingLabPermissionsAsync(_testUserId, _testTenantId);
        Assert.False(permissions.CanCreateSessions);
        Assert.False(permissions.CanEditSessions);
        Assert.False(permissions.CanDeleteSessions);
        Assert.False(permissions.CanManageTesters);
        Assert.False(permissions.CanViewReports);
        Assert.False(permissions.CanExportData);
        Assert.False(permissions.CanAdminister);
        Assert.Contains("TestingLabTester", permissions.AssignedRoles);
    }

    [Fact]
    public async Task RevokeRole_ShouldRemovePermissions()
    {
        // Arrange - Assign role
        await _modulePermissionService.AssignRoleAsync(
            _testUserId, 
            _testTenantId, 
            ModuleType.TestingLab, 
            "TestingLabAdmin");
        
        // Verify role is assigned
        Assert.True(await _modulePermissionService.CanDeleteTestingSessionsAsync(_testUserId, _testTenantId));

        // Act - Revoke role
        var revoked = await _modulePermissionService.RevokeRoleAsync(
            _testUserId, 
            _testTenantId, 
            ModuleType.TestingLab, 
            "TestingLabAdmin");

        // Assert
        Assert.True(revoked);
        Assert.False(await _modulePermissionService.CanDeleteTestingSessionsAsync(_testUserId, _testTenantId));
        
        var permissions = await _modulePermissionService.GetUserTestingLabPermissionsAsync(_testUserId, _testTenantId);
        Assert.Empty(permissions.AssignedRoles);
    }

    [Fact]
    public async Task GetUsersWithRole_ShouldReturnCorrectUsers()
    {
        // Arrange
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();
        
        await _modulePermissionService.AssignRoleAsync(user1, _testTenantId, ModuleType.TestingLab, "TestingLabAdmin");
        await _modulePermissionService.AssignRoleAsync(user2, _testTenantId, ModuleType.TestingLab, "TestingLabAdmin");
        await _modulePermissionService.AssignRoleAsync(user3, _testTenantId, ModuleType.TestingLab, "TestingLabManager");

        // Act
        var admins = await _modulePermissionService.GetUsersWithRoleAsync(_testTenantId, ModuleType.TestingLab, "TestingLabAdmin");
        var managers = await _modulePermissionService.GetUsersWithRoleAsync(_testTenantId, ModuleType.TestingLab, "TestingLabManager");

        // Assert
        Assert.Equal(2, admins.Count);
        Assert.Contains(admins, a => a.UserId == user1);
        Assert.Contains(admins, a => a.UserId == user2);
        
        Assert.Single(managers);
        Assert.Contains(managers, m => m.UserId == user3);
    }

    [Fact]
    public async Task GetModuleRoleDefinitions_ShouldReturnSystemRoles()
    {
        // Act
        var roles = await _modulePermissionService.GetModuleRoleDefinitionsAsync(ModuleType.TestingLab);

        // Assert
        Assert.Equal(4, roles.Count); // Admin, Manager, Coordinator, Tester
        Assert.Contains(roles, r => r.Name == "TestingLabAdmin");
        Assert.Contains(roles, r => r.Name == "TestingLabManager");
        Assert.Contains(roles, r => r.Name == "TestingLabCoordinator");
        Assert.Contains(roles, r => r.Name == "TestingLabTester");
        
        // Check role priorities
        var adminRole = roles.First(r => r.Name == "TestingLabAdmin");
        var testerRole = roles.First(r => r.Name == "TestingLabTester");
        Assert.True(adminRole.Priority > testerRole.Priority);
    }

    [Fact]
    public async Task MultipleRoles_ShouldGrantCombinedPermissions()
    {
        // Arrange - Assign multiple roles to same user
        await _modulePermissionService.AssignRoleAsync(_testUserId, _testTenantId, ModuleType.TestingLab, "TestingLabTester");
        await _modulePermissionService.AssignRoleAsync(_testUserId, _testTenantId, ModuleType.TestingLab, "TestingLabCoordinator");

        // Act & Assert - Should have coordinator permissions (higher role)
        Assert.True(await _modulePermissionService.CanCreateTestingSessionsAsync(_testUserId, _testTenantId));
        Assert.True(await _modulePermissionService.CanManageTestersAsync(_testUserId, _testTenantId));
        Assert.False(await _modulePermissionService.CanDeleteTestingSessionsAsync(_testUserId, _testTenantId));
        
        var roles = await _modulePermissionService.GetUserRolesAsync(_testUserId, _testTenantId, ModuleType.TestingLab);
        Assert.Equal(2, roles.Count);
    }
}
