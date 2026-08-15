using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class RolePermissionProviderTests
{
    [Fact]
    public async Task GetPermissionsAsync_ReturnsDistinctActiveGlobalAndTenantRolePermissions()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = new Mock<IRoleRepository>();
        repository
            .Setup(instance => instance.GetUserRolesAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Role("Global", "Global role") { Permissions = "[\"courses:read\",\"courses:update\"]" },
                new Role("Tenant", "Tenant role", tenantId) { Permissions = "[\"courses:update\",\"users:read\"]" },
                new Role("Other", "Other tenant", Guid.NewGuid()) { Permissions = "[\"platform:billing\"]" }
            ]);

        var provider = new RolePermissionProvider(repository.Object);

        var result = await provider.GetPermissionsAsync(userId, tenantId);

        result.Should().BeEquivalentTo("courses:read", "courses:update", "users:read");
    }

    [Fact]
    public async Task GetPermissionsAsync_DoesNotProjectUniversalAdminWildcard()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = new Mock<IRoleRepository>();
        repository
            .Setup(instance => instance.GetUserRolesAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Role("Unsafe", "Legacy wildcard role", tenantId)
                {
                    Permissions = "[\"admin:*\",\"courses:read\"]"
                }
            ]);

        var provider = new RolePermissionProvider(repository.Object);

        var result = await provider.GetPermissionsAsync(userId, tenantId);

        result.Should().Equal("courses:read");
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    public async Task GetPermissionsAsync_InvalidOrNullJson_ReturnsNoPermissions(string permissions)
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = new Mock<IRoleRepository>();
        repository
            .Setup(instance => instance.GetUserRolesAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Role("Role", "Role", tenantId) { Permissions = permissions }]);

        var result = await new RolePermissionProvider(repository.Object)
            .GetPermissionsAsync(userId, tenantId);

        result.Should().BeEmpty();
    }
}
