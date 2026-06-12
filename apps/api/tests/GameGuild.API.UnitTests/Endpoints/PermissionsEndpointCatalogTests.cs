using FluentAssertions;
using GameGuild.API.Endpoints;
using GameGuild.Identity.Authorization;

namespace GameGuild.API.UnitTests.Endpoints;

public sealed class PermissionsEndpointCatalogTests
{
    [Fact]
    public void List_ShouldExposeRegisteredPermissionMetadata()
    {
        var permissions = PermissionsEndpointCatalog.List();

        permissions.Should().Contain(permission =>
            permission.Name == Permissions.UsersRead &&
            permission.Resource == "users" &&
            permission.Action == "read" &&
            !string.IsNullOrWhiteSpace(permission.Description));
    }

    [Fact]
    public void GetById_ShouldUseDeterministicIdsForRegisteredPermissionsOnly()
    {
        var usersReadId = PermissionsEndpointCatalog.GetStableId(Permissions.UsersRead);

        var permission = PermissionsEndpointCatalog.GetById(usersReadId);

        permission.Should().NotBeNull();
        permission!.Name.Should().Be(Permissions.UsersRead);
        PermissionsEndpointCatalog.GetById(Guid.NewGuid()).Should().BeNull();
    }

    [Fact]
    public void ValidatePermissionIds_ShouldReturnInvalidIdsInsteadOfPretendingAssignmentSucceeded()
    {
        var validId = PermissionsEndpointCatalog.GetStableId(Permissions.UsersRead);
        var invalidId = Guid.NewGuid();

        var validation = PermissionsEndpointCatalog.ValidatePermissionIds([validId, invalidId]);

        validation.ValidPermissions.Should().ContainSingle(permission => permission.Name == Permissions.UsersRead);
        validation.InvalidPermissionIds.Should().ContainSingle().Which.Should().Be(invalidId);
    }
}
