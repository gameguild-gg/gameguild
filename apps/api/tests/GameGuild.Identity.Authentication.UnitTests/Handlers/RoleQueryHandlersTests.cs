using AutoFixture;
using FluentAssertions;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authentication;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Handlers;

public class RoleQueryHandlersTests
{
    private readonly Mock<IRoleRepository> _mockRepository;
    private readonly Fixture _fixture;

    public RoleQueryHandlersTests()
    {
        _mockRepository = new Mock<IRoleRepository>();
        _fixture = new Fixture();
    }

    #region GetRolesQueryHandler Tests

    [Fact]
    public async Task GetRolesQueryHandler_WithoutFilters_ReturnsAllRoles()
    {
        // Arrange
        var roles = new List<Role>
        {
            new Role("Admin", "Admin role", null) { Id = Guid.NewGuid(), Permissions = "[\"read\",\"write\"]" },
            new Role("User", "User role", null) { Id = Guid.NewGuid(), Permissions = "[\"read\"]" }
        };

        var query = new GetRolesQuery
        {
            TenantId = null,
            IncludeInactive = false
        };

        _mockRepository.Setup(r => r.GetAllAsync(query.TenantId, query.IncludeInactive, default))
            .ReturnsAsync(roles);

        var handler = new GetRolesQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Name == "Admin");
        result.Should().Contain(r => r.Name == "User");
        
        var adminRole = result.First(r => r.Name == "Admin");
        adminRole.Permissions.Should().BeEquivalentTo(new[] { "read", "write" });
    }

    [Fact]
    public async Task GetRolesQueryHandler_WithTenantFilter_ReturnsOnlyTenantRoles()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var roles = new List<Role>
        {
            new Role("TenantRole", "Tenant role", tenantId) { Id = Guid.NewGuid(), Permissions = "[]" }
        };

        var query = new GetRolesQuery
        {
            TenantId = tenantId,
            IncludeInactive = false
        };

        _mockRepository.Setup(r => r.GetAllAsync(tenantId, false, default))
            .ReturnsAsync(roles);

        var handler = new GetRolesQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Should().HaveCount(1);
        result.First().TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task GetRolesQueryHandler_WithIncludeInactive_ReturnsAllRoles()
    {
        // Arrange
        var roles = new List<Role>
        {
            new Role("Active", "Active role", null) { Id = Guid.NewGuid(), IsActive = true, Permissions = "[]" },
            new Role("Inactive", "Inactive role", null) { Id = Guid.NewGuid(), IsActive = false, Permissions = "[]" }
        };

        var query = new GetRolesQuery
        {
            TenantId = null,
            IncludeInactive = true
        };

        _mockRepository.Setup(r => r.GetAllAsync(null, true, default))
            .ReturnsAsync(roles);

        var handler = new GetRolesQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.IsActive);
        result.Should().Contain(r => !r.IsActive);
    }

    [Fact]
    public async Task GetRolesQueryHandler_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetRolesQuery();

        _mockRepository.Setup(r => r.GetAllAsync(null, false, default))
            .ReturnsAsync(new List<Role>());

        var handler = new GetRolesQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetRoleByIdQueryHandler Tests

    [Fact]
    public async Task GetRoleByIdQueryHandler_RoleExists_ReturnsRole()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new Role("TestRole", "Test Description", null) 
        { 
            Id = roleId,
            Permissions = "[\"admin\",\"read\",\"write\"]",
            IsActive = true
        };

        var query = new GetRoleByIdQuery { RoleId = roleId };

        _mockRepository.Setup(r => r.GetByIdAsync(roleId, default))
            .ReturnsAsync(role);

        var handler = new GetRoleByIdQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(roleId);
        result.Name.Should().Be("TestRole");
        result.Description.Should().Be("Test Description");
        result.Permissions.Should().BeEquivalentTo(new[] { "admin", "read", "write" });
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetRoleByIdQueryHandler_RoleNotFound_ReturnsNull()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var query = new GetRoleByIdQuery { RoleId = roleId };

        _mockRepository.Setup(r => r.GetByIdAsync(roleId, default))
            .ReturnsAsync((Role?)null);

        var handler = new GetRoleByIdQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRoleByIdQueryHandler_RoleWithEmptyPermissions_ReturnsEmptyList()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new Role("RoleWithNoPermissions", "Description", null) 
        { 
            Id = roleId,
            Permissions = "[]"
        };

        var query = new GetRoleByIdQuery { RoleId = roleId };

        _mockRepository.Setup(r => r.GetByIdAsync(roleId, default))
            .ReturnsAsync(role);

        var handler = new GetRoleByIdQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Should().NotBeNull();
        result!.Permissions.Should().BeEmpty();
    }

    #endregion

    #region GetUserRolesQueryHandler Tests

    [Fact]
    public async Task GetUserRolesQueryHandler_UserHasRoles_ReturnsRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roles = new List<Role>
        {
            new Role("Role1", "Description 1", null) { Id = Guid.NewGuid(), Permissions = "[\"read\"]" },
            new Role("Role2", "Description 2", null) { Id = Guid.NewGuid(), Permissions = "[\"write\"]" }
        };

        var query = new GetUserRolesQuery
        {
            UserId = userId,
            IncludeExpired = false
        };

        _mockRepository.Setup(r => r.GetUserRolesAsync(userId, false, default))
            .ReturnsAsync(roles);

        var handler = new GetUserRolesQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Name == "Role1");
        result.Should().Contain(r => r.Name == "Role2");
    }

    [Fact]
    public async Task GetUserRolesQueryHandler_WithIncludeExpired_ReturnsAllRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roles = new List<Role>
        {
            new Role("ActiveRole", "Active", null) { Id = Guid.NewGuid(), Permissions = "[]" },
            new Role("ExpiredRole", "Expired", null) { Id = Guid.NewGuid(), Permissions = "[]" }
        };

        var query = new GetUserRolesQuery
        {
            UserId = userId,
            IncludeExpired = true
        };

        _mockRepository.Setup(r => r.GetUserRolesAsync(userId, true, default))
            .ReturnsAsync(roles);

        var handler = new GetUserRolesQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserRolesQueryHandler_UserHasNoRoles_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserRolesQuery { UserId = userId };

        _mockRepository.Setup(r => r.GetUserRolesAsync(userId, false, default))
            .ReturnsAsync(new List<Role>());

        var handler = new GetUserRolesQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserRolesQueryHandler_DeserializesPermissionsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roles = new List<Role>
        {
            new Role("AdminRole", "Admin", null) 
            { 
                Id = Guid.NewGuid(), 
                Permissions = "[\"users:read\",\"users:write\",\"users:delete\"]" 
            }
        };

        var query = new GetUserRolesQuery { UserId = userId };

        _mockRepository.Setup(r => r.GetUserRolesAsync(userId, false, default))
            .ReturnsAsync(roles);

        var handler = new GetUserRolesQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Should().HaveCount(1);
        result.First().Permissions.Should().BeEquivalentTo(new[] { "users:read", "users:write", "users:delete" });
    }

    #endregion
}
