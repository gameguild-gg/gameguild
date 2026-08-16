using FluentAssertions;
using GameGuild.API.Database;
using Xunit;
using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

using GameGuild.Tests.Authentication.Integration.TestHelpers;

namespace GameGuild.Tests.Authentication.Integration;

/// <summary>
/// Integration tests for Role Management functionality
/// Tests role-user assignments, temporary role expiration, and multi-tenancy isolation
/// </summary>
public class RoleManagementIntegrationTests : IClassFixture<AuthenticationApiFactory>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly IServiceScope _scope;
    private readonly IRoleRepository _roleRepository;
    private readonly ApplicationDbContext _dbContext;

    public RoleManagementIntegrationTests(AuthenticationApiFactory factory)
    {
        _factory = factory;

        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _roleRepository = _scope.ServiceProvider.GetRequiredService<IRoleRepository>();

        // Ensure database is created
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        try
        {
            _dbContext?.Database.EnsureDeleted();
        }
        catch (ObjectDisposedException)
        {
            // Database already disposed
        }
        finally
        {
            _dbContext?.Dispose();
            _scope?.Dispose();
        }
    }

    private Role CreateTestRole(string name, Guid? tenantId = null)
    {
        return new Role(name, $"{name} description", tenantId)
        {
            Permissions = JsonSerializer.Serialize(new List<string> { "read", "write" })
        };
    }

    #region Role-User Assignment Tests

    [Fact]
    public async Task AssignRoleToUser_ShouldPersistInDatabase()
    {
        // Arrange
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "TestRole",
            Description = "Test role for assignment",
            IsActive = true,
            Permissions = JsonSerializer.Serialize(new List<string> { "read", "write" })
        };

        var userId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();

        await _roleRepository.AddAsync(role);

        // Act
        var userRole = new UserRole(userId, role.Id, assignedBy);
        await _roleRepository.AssignRoleToUserAsync(userRole);

        // Assert
        var userRoles = await _roleRepository.GetUserRolesAsync(userId);
        userRoles.Should().HaveCount(1);
        userRoles.First().Id.Should().Be(role.Id);
        userRoles.First().Name.Should().Be("TestRole");
    }

    [Fact]
    public async Task AssignMultipleRolesToUser_ShouldPersistAllRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();

        var roles = new List<Role>
        {
            new Role { Id = Guid.NewGuid(), Name = "Admin", IsActive = true },
            new Role { Id = Guid.NewGuid(), Name = "Editor", IsActive = true },
            new Role { Id = Guid.NewGuid(), Name = "Viewer", IsActive = true }
        };

        foreach (var role in roles)
        {
            await _roleRepository.AddAsync(role);
            var userRole = new UserRole(userId, role.Id, assignedBy);
            await _roleRepository.AssignRoleToUserAsync(userRole);
        }

        // Act
        var userRoles = await _roleRepository.GetUserRolesAsync(userId);

        // Assert
        userRoles.Should().HaveCount(3);
        userRoles.Select(r => r.Name).Should().BeEquivalentTo(new[] { "Admin", "Editor", "Viewer" });
    }

    [Fact]
    public async Task RemoveRoleFromUser_ShouldDeleteAssignment()
    {
        // Arrange
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "TemporaryRole",
            IsActive = true
        };

        var userId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();

        await _roleRepository.AddAsync(role);
        var userRole = new UserRole(userId, role.Id, assignedBy);
        await _roleRepository.AssignRoleToUserAsync(userRole);

        // Act
        await _roleRepository.RemoveRoleFromUserAsync(userId, role.Id);

        // Assert
        // Role removed successfully
        var userRoles = await _roleRepository.GetUserRolesAsync(userId);
        userRoles.Should().BeEmpty();
    }

    [Fact]
    public async Task AssignRoleToMultipleUsers_ShouldPersistAllAssignments()
    {
        // Arrange
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "SharedRole",
            IsActive = true
        };

        await _roleRepository.AddAsync(role);

        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var assignedBy = Guid.NewGuid();

        // Act
        foreach (var userId in userIds)
        {
            var userRole = new UserRole(userId, role.Id, assignedBy);
            await _roleRepository.AssignRoleToUserAsync(userRole);
        }

        // Assert
        foreach (var userId in userIds)
        {
            var userRoles = await _roleRepository.GetUserRolesAsync(userId);
            userRoles.Should().ContainSingle(r => r.Id == role.Id);
        }
    }

    [Fact]
    public async Task AssignDuplicateRole_ShouldNotCreateMultipleAssignments()
    {
        // Arrange
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "UniqueRole",
            IsActive = true
        };

        var userId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();

        await _roleRepository.AddAsync(role);
        var userRole = new UserRole(userId, role.Id, assignedBy);
        await _roleRepository.AssignRoleToUserAsync(userRole);

        // Act - Try to assign the same role again
        var userRole2 = new UserRole(userId, role.Id, assignedBy);
        await _roleRepository.AssignRoleToUserAsync(userRole2);

        // Assert - Should still have only one assignment
        var userRoles = await _roleRepository.GetUserRolesAsync(userId);
        userRoles.Should().HaveCount(1);
    }

    #endregion

    #region Temporary Role Expiration Tests

    [Fact]
    public async Task AssignTemporaryRole_WithExpirationDate_ShouldPersist()
    {
        // Arrange
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "TemporaryAccessRole",
            IsActive = true
        };

        var userId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(30);

        await _roleRepository.AddAsync(role);

        // Act
        var userRole = new UserRole(userId, role.Id, assignedBy) { ExpiresAt = expiresAt };
        await _roleRepository.AssignRoleToUserAsync(userRole);

        // Assert
        var assignment = await _dbContext.Set<UserRole>()
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);

        assignment.Should().NotBeNull();
        assignment!.ExpiresAt.Should().NotBeNull();
        assignment.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetUserRoles_ShouldExcludeExpiredRoles()
    {
        // Arrange
        var activeRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "ActiveRole",
            IsActive = true
        };

        var expiredRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "ExpiredRole",
            IsActive = true
        };

        var userId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();

        await _roleRepository.AddAsync(activeRole);
        await _roleRepository.AddAsync(expiredRole);

        // Assign active role (no expiration)
        var userRole = new UserRole(userId, activeRole.Id, assignedBy);
        await _roleRepository.AssignRoleToUserAsync(userRole);

        // Assign expired role (expired yesterday)
        var expiredUserRole = new UserRole(userId, expiredRole.Id, assignedBy)
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        await _roleRepository.AssignRoleToUserAsync(expiredUserRole);

        // Act
        var userRoles = await _roleRepository.GetUserRolesAsync(userId);

        // Assert
        userRoles.Should().HaveCount(1);
        userRoles.First().Name.Should().Be("ActiveRole");
    }

    [Fact]
    public async Task GetUserRoles_ShouldIncludeRolesExpiringInFuture()
    {
        // Arrange
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "FutureExpiryRole",
            IsActive = true
        };

        var userId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(7);

        await _roleRepository.AddAsync(role);
        var userRole = new UserRole(userId, role.Id, assignedBy) { ExpiresAt = expiresAt };
        await _roleRepository.AssignRoleToUserAsync(userRole);

        // Act
        var userRoles = await _roleRepository.GetUserRolesAsync(userId);

        // Assert
        userRoles.Should().HaveCount(1);
        userRoles.First().Name.Should().Be("FutureExpiryRole");
    }

    [Fact]
    public async Task TemporaryRoleExpiration_ShouldBeExactlyAtExpiryTime()
    {
        // Arrange
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "PreciseExpiryRole",
            IsActive = true
        };

        var userId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        await _roleRepository.AddAsync(role);
        var userRole = new UserRole(userId, role.Id, assignedBy) { ExpiresAt = expiresAt };
        await _roleRepository.AssignRoleToUserAsync(userRole);

        // Act - Check before expiration
        var rolesBeforeExpiry = await _roleRepository.GetUserRolesAsync(userId);

        // Simulate time passing by updating the expiration to the past
        var assignment = await _dbContext.Set<UserRole>()
            .FirstAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);
        assignment.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        await _dbContext.SaveChangesAsync();

        // Act - Check after expiration
        var rolesAfterExpiry = await _roleRepository.GetUserRolesAsync(userId);

        // Assert
        rolesBeforeExpiry.Should().HaveCount(1);
        rolesAfterExpiry.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveTemporaryRole_BeforeExpiration_ShouldSucceed()
    {
        // Arrange
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "EarlyRemovalRole",
            IsActive = true
        };

        var userId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(30);

        await _roleRepository.AddAsync(role);
        var userRole = new UserRole(userId, role.Id, assignedBy) { ExpiresAt = expiresAt };
        await _roleRepository.AssignRoleToUserAsync(userRole);

        // Act
        await _roleRepository.RemoveRoleFromUserAsync(userId, role.Id);

        // Assert
        // Role removed successfully
        var userRoles = await _roleRepository.GetUserRolesAsync(userId);
        userRoles.Should().BeEmpty();
    }

    #endregion

    #region Multi-Tenancy Isolation Tests

    [Fact]
    public async Task GetRolesByTenant_ShouldOnlyReturnTenantRoles()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();

        var tenant1Roles = new List<Role>
        {
            TestEntityFactory.CreateRole("Tenant1Admin", tenant1Id, "Admin for tenant 1", new List<string> { "read", "write" }),
            TestEntityFactory.CreateRole("Tenant1User", tenant1Id, "User for tenant 1", new List<string> { "read" })
        };

        var tenant2Roles = new List<Role>
        {
            TestEntityFactory.CreateRole("Tenant2Admin", tenant2Id, "Admin for tenant 2", new List<string> { "read", "write" }),
            TestEntityFactory.CreateRole("Tenant2User", tenant2Id, "User for tenant 2", new List<string> { "read" })
        };

        var globalRole = TestEntityFactory.CreateRole("GlobalRole", null, "Global role", new List<string> { "read" });

        foreach (var role in tenant1Roles.Concat(tenant2Roles).Append(globalRole))
        {
            await _roleRepository.AddAsync(role);
        }

        // Act
        var tenant1Result = await _roleRepository.GetAllAsync(tenant1Id);
        var tenant2Result = await _roleRepository.GetAllAsync(tenant2Id);

        // Assert
        tenant1Result.Should().HaveCount(2);
        tenant1Result.Select(r => r.Name).Should().BeEquivalentTo(new[] { "Tenant1Admin", "Tenant1User" });

        tenant2Result.Should().HaveCount(2);
        tenant2Result.Select(r => r.Name).Should().BeEquivalentTo(new[] { "Tenant2Admin", "Tenant2User" });
    }

    [Fact]
    public async Task GetRoleByName_ShouldRespectTenantIsolation()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();

        var tenant1Role = TestEntityFactory.CreateRole("Manager", tenant1Id, "Manager role for tenant 1", new List<string> { "read", "write" });

        var tenant2Role = TestEntityFactory.CreateRole("Manager", tenant2Id, "Manager role for tenant 2", new List<string> { "read", "write" });

        await _roleRepository.AddAsync(tenant1Role);
        await _roleRepository.AddAsync(tenant2Role);

        // Act
        var tenant1Manager = await _roleRepository.GetByNameAsync("Manager", tenant1Id);
        var tenant2Manager = await _roleRepository.GetByNameAsync("Manager", tenant2Id);

        // Assert
        tenant1Manager.Should().NotBeNull();
        tenant1Manager!.Id.Should().Be(tenant1Role.Id);
        tenant1Manager.TenantId.Should().Be(tenant1Id);

        tenant2Manager.Should().NotBeNull();
        tenant2Manager!.Id.Should().Be(tenant2Role.Id);
        tenant2Manager.TenantId.Should().Be(tenant2Id);
    }

    [Fact]
    public async Task CreateRole_WithSameNameInDifferentTenants_ShouldSucceed()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();

        var role1 = TestEntityFactory.CreateRole("Editor", tenant1Id, "Editor role for tenant 1", new List<string> { "read", "write" });

        var role2 = TestEntityFactory.CreateRole("Editor", tenant2Id, "Editor role for tenant 2", new List<string> { "read", "write" });

        // Act
        await _roleRepository.AddAsync(role1);
        await _roleRepository.AddAsync(role2);

        // Assert
        var tenant1Role = await _roleRepository.GetByNameAsync("Editor", tenant1Id);
        var tenant2Role = await _roleRepository.GetByNameAsync("Editor", tenant2Id);

        tenant1Role.Should().NotBeNull();
        tenant2Role.Should().NotBeNull();
        tenant1Role!.Id.Should().NotBe(tenant2Role!.Id);
    }

    [Fact]
    public async Task UpdateRole_ShouldNotAffectOtherTenants()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();

        var tenant1Role = TestEntityFactory.CreateRole("Viewer", tenant1Id, "Original description", new List<string> { "read", "write" });

        var tenant2Role = TestEntityFactory.CreateRole("Viewer", tenant2Id, "Original description", new List<string> { "read", "write" });

        await _roleRepository.AddAsync(tenant1Role);
        await _roleRepository.AddAsync(tenant2Role);

        // Act - Update tenant1's role
        tenant1Role.Description = "Updated description";
        await _roleRepository.UpdateAsync(tenant1Role);

        // Assert
        var tenant1Updated = await _roleRepository.GetByIdAsync(tenant1Role.Id);
        var tenant2Unchanged = await _roleRepository.GetByIdAsync(tenant2Role.Id);

        tenant1Updated!.Description.Should().Be("Updated description");
        tenant2Unchanged!.Description.Should().Be("Original description");
    }

    [Fact]
    public async Task DeleteRole_ShouldOnlyDeleteInSpecificTenant()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();

        var tenant1Role = TestEntityFactory.CreateRole("Contributor", tenant1Id, "Contributor role for tenant 1", new List<string> { "read", "write" });

        var tenant2Role = TestEntityFactory.CreateRole("Contributor", tenant2Id, "Contributor role for tenant 2", new List<string> { "read", "write" });

        await _roleRepository.AddAsync(tenant1Role);
        await _roleRepository.AddAsync(tenant2Role);

        // Act - Delete tenant1's role
        await _roleRepository.DeleteAsync(tenant1Role.Id);

        // Assert
        var tenant1Deleted = await _roleRepository.GetByIdAsync(tenant1Role.Id);
        var tenant2Exists = await _roleRepository.GetByIdAsync(tenant2Role.Id);

        tenant1Deleted.Should().BeNull();
        tenant2Exists.Should().NotBeNull();
    }

    [Fact]
    public async Task GlobalRole_ShouldNotBeReturnedWithTenantFilter()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        var globalRole = TestEntityFactory.CreateRole("SuperAdmin", null, "Global SuperAdmin role", new List<string> { "read", "write", "delete" });

        var tenantRole = TestEntityFactory.CreateRole("TenantAdmin", tenantId, "Tenant-specific Admin role", new List<string> { "read", "write" });

        await _roleRepository.AddAsync(globalRole);
        await _roleRepository.AddAsync(tenantRole);

        // Act
        var tenantRoles = await _roleRepository.GetAllAsync(tenantId);

        // Assert
        tenantRoles.Should().HaveCount(1);
        tenantRoles.First().Name.Should().Be("TenantAdmin");
        tenantRoles.Should().NotContain(r => r.Name == "SuperAdmin");
    }

    [Fact]
    public async Task UserRoleAssignment_ShouldWorkAcrossTenants()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();

        var tenant1Role = TestEntityFactory.CreateRole("Tenant1Role", tenant1Id, "Role for tenant 1", new List<string> { "read", "write" });

        var tenant2Role = TestEntityFactory.CreateRole("Tenant2Role", tenant2Id, "Role for tenant 2", new List<string> { "read", "write" });

        await _roleRepository.AddAsync(tenant1Role);
        await _roleRepository.AddAsync(tenant2Role);

        // Act - Assign roles from different tenants to same user
        var userRole = new UserRole(userId, tenant1Role.Id, assignedBy);
        await _roleRepository.AssignRoleToUserAsync(userRole);
        var userRole2 = new UserRole(userId, tenant2Role.Id, assignedBy);
        await _roleRepository.AssignRoleToUserAsync(userRole2);

        // Assert
        var userRoles = await _roleRepository.GetUserRolesAsync(userId);
        userRoles.Should().HaveCount(2);
        userRoles.Select(r => r.Name).Should().BeEquivalentTo(new[] { "Tenant1Role", "Tenant2Role" });
    }

    #endregion

    #region Combined Scenarios

    [Fact]
    public async Task TemporaryRoleInMultiTenantEnvironment_ShouldWorkCorrectly()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();

        var tenant1Role = TestEntityFactory.CreateRole("TemporaryAccess", tenant1Id, "Temporary access role", new List<string> { "read" });

        var tenant2Role = TestEntityFactory.CreateRole("PermanentAccess", tenant2Id, "Permanent access role", new List<string> { "read", "write" });

        await _roleRepository.AddAsync(tenant1Role);
        await _roleRepository.AddAsync(tenant2Role);

        // Assign temporary role in tenant1
        var temporaryUserRole = new UserRole(userId, tenant1Role.Id, assignedBy) { ExpiresAt = DateTime.UtcNow.AddDays(7) };
        await _roleRepository.AssignRoleToUserAsync(temporaryUserRole);

        // Assign permanent role in tenant2
        var userRole = new UserRole(userId, tenant2Role.Id, assignedBy);
        await _roleRepository.AssignRoleToUserAsync(userRole);

        // Act
        var userRoles = await _roleRepository.GetUserRolesAsync(userId);

        // Assert
        userRoles.Should().HaveCount(2);
        userRoles.Should().Contain(r => r.Name == "TemporaryAccess");
        userRoles.Should().Contain(r => r.Name == "PermanentAccess");
    }

    #endregion
}
