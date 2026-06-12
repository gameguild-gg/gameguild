using FluentAssertions;
using GameGuild.CQRS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests;

public class ResourcePermissionServiceUserLookupTests
{
    [Fact]
    public async Task ShareResourceAsync_WhenEmailBelongsToExistingUser_GrantsDirectPermission()
    {
        var tenantId = TenantId.New();
        var userId = Guid.NewGuid();
        var permissions = new List<ResourceUserPermission>();
        var invitations = new List<ResourceInvitation>();
        var dbContext = CreateDbContext(permissions, invitations);
        var lookup = new Mock<IResourceShareUserLookup>();
        lookup
            .Setup(x => x.FindByEmailAsync(tenantId, "learner@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceShareUser(userId, "learner@example.com", "Learner One"));

        var service = new ResourcePermissionService(
            dbContext.Object,
            NullLogger<ResourcePermissionService>.Instance,
            userLookup: lookup.Object);

        var result = await service.ShareResourceAsync(
            tenantId,
            "projects",
            "project-1",
            new ShareResourceRequest("learner@example.com", ["read", "comment"]),
            Guid.NewGuid());

        result.Success.Should().BeTrue();
        result.IsNewUser.Should().BeFalse();
        result.UserId.Should().Be(userId);
        permissions.Should().ContainSingle();
        permissions[0].UserId.Should().Be(userId);
        permissions[0].Permissions.Should().Equal("read", "comment");
        invitations.Should().BeEmpty();
    }

    [Fact]
    public async Task GetResourceUsersAsync_EnrichesDirectPermissionsFromUserLookup()
    {
        var tenantId = TenantId.New();
        var userId = Guid.NewGuid();
        var permissions = new List<ResourceUserPermission>
        {
            new()
            {
                TenantId = tenantId,
                UserId = userId,
                ResourceType = "projects",
                ResourceId = "project-1",
                Permissions = ["read"],
                GrantedByUserId = Guid.NewGuid()
            }
        };
        var dbContext = CreateDbContext(permissions, []);
        var lookup = new Mock<IResourceShareUserLookup>();
        lookup
            .Setup(x => x.FindByIdAsync(tenantId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceShareUser(userId, "learner@example.com", "Learner One"));

        var service = new ResourcePermissionService(
            dbContext.Object,
            NullLogger<ResourcePermissionService>.Instance,
            userLookup: lookup.Object);

        var result = await service.GetResourceUsersAsync(tenantId, "projects", "project-1");

        result.Users.Should().ContainSingle();
        result.Users[0].UserName.Should().Be("Learner One");
        result.Users[0].Email.Should().Be("learner@example.com");
        result.TotalCount.Should().Be(1);
    }

    private static Mock<IApplicationDbContext> CreateDbContext(
        List<ResourceUserPermission> permissions,
        List<ResourceInvitation> invitations)
    {
        var permissionSet = permissions.AsQueryable().BuildMockDbSet();
        permissionSet
            .Setup(set => set.Add(It.IsAny<ResourceUserPermission>()))
            .Callback<ResourceUserPermission>(permissions.Add)
            .Returns((EntityEntry<ResourceUserPermission>)null!);

        var invitationSet = invitations.AsQueryable().BuildMockDbSet();
        invitationSet
            .Setup(set => set.Add(It.IsAny<ResourceInvitation>()))
            .Callback<ResourceInvitation>(invitations.Add)
            .Returns((EntityEntry<ResourceInvitation>)null!);

        var dbContext = new Mock<IApplicationDbContext>();
        dbContext.Setup(context => context.Set<ResourceUserPermission>()).Returns(permissionSet.Object);
        dbContext.Setup(context => context.Set<ResourceInvitation>()).Returns(invitationSet.Object);
        dbContext.Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return dbContext;
    }
}
