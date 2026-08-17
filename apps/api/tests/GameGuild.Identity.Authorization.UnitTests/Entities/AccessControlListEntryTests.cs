using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Entities;

public class AccessControlListEntryTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _grantedBy = Guid.NewGuid();

#pragma warning disable CS0618
    [Fact]
    public void UserId_BackwardCompatibility_MapsOnlyUserPrincipals()
    {
        var userId = Guid.NewGuid();
        var entry = new AccessControlListEntry { UserId = userId };

        entry.PrincipalType.Should().Be(AclPrincipalType.User);
        entry.PrincipalId.Should().Be(userId);
        entry.UserId.Should().Be(userId);

        entry.PrincipalType = AclPrincipalType.Role;
        entry.UserId.Should().BeEmpty();
        entry.PrincipalType = AclPrincipalType.User;
        entry.PrincipalId = null;
        entry.UserId.Should().BeEmpty();
    }
#pragma warning restore CS0618

    [Fact]
    public void AccessControlListEntry_DefaultValues_ShouldBeCorrect()
    {
        var entry = new AccessControlListEntry();

        entry.PrincipalType.Should().Be(AclPrincipalType.User);
        entry.PrincipalId.Should().BeNull();
        entry.ResourceType.Should().BeEmpty();
        entry.ResourceId.Should().BeEmpty();
        entry.AccessLevel.Should().Be(AccessLevel.None);
        entry.IsDenied.Should().BeFalse();
        entry.IsActive.Should().BeTrue();
        entry.ExpiresAt.Should().BeNull();
        entry.Notes.Should().BeNull();
    }

    [Fact]
    public void IsEffective_WhenActiveAndNotExpired_ShouldBeTrue()
    {
        var entry = new AccessControlListEntry
        {
            IsActive = true,
            ExpiresAt = null
        };

        entry.IsEffective.Should().BeTrue();
    }

    [Fact]
    public void IsEffective_WhenActiveAndExpiresInFuture_ShouldBeTrue()
    {
        var entry = new AccessControlListEntry
        {
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        entry.IsEffective.Should().BeTrue();
    }

    [Fact]
    public void IsEffective_WhenInactive_ShouldBeFalse()
    {
        var entry = new AccessControlListEntry
        {
            IsActive = false,
            ExpiresAt = null
        };

        entry.IsEffective.Should().BeFalse();
    }

    [Fact]
    public void IsEffective_WhenExpired_ShouldBeFalse()
    {
        var entry = new AccessControlListEntry
        {
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        entry.IsEffective.Should().BeFalse();
    }

    [Fact]
    public void IsEffective_WhenInactiveAndExpired_ShouldBeFalse()
    {
        var entry = new AccessControlListEntry
        {
            IsActive = false,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        entry.IsEffective.Should().BeFalse();
    }

    [Fact]
    public void ForUser_ShouldCreateUserBasedEntry()
    {
        var entry = AccessControlListEntry.ForUser(
            _tenantId, _userId, "Course", "course-1",
            AccessLevel.Write, _grantedBy);

        entry.TenantId.Should().Be(_tenantId);
        entry.PrincipalType.Should().Be(AclPrincipalType.User);
        entry.PrincipalId.Should().Be(_userId);
        entry.ResourceType.Should().Be("Course");
        entry.ResourceId.Should().Be("course-1");
        entry.AccessLevel.Should().Be(AccessLevel.Write);
        entry.IsDenied.Should().BeFalse();
        entry.GrantedBy.Should().Be(_grantedBy);
        entry.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ForUser_WithDeny_ShouldCreateDenyEntry()
    {
        var entry = AccessControlListEntry.ForUser(
            _tenantId, _userId, "Course", "course-1",
            AccessLevel.Write, _grantedBy, isDenied: true);

        entry.IsDenied.Should().BeTrue();
        entry.PrincipalType.Should().Be(AclPrincipalType.User);
    }

    [Fact]
    public void ForRole_ShouldCreateRoleBasedEntry()
    {
        var roleId = Guid.NewGuid();
        var entry = AccessControlListEntry.ForRole(
            _tenantId, roleId, "Project", "proj-1",
            AccessLevel.Admin, _grantedBy);

        entry.PrincipalType.Should().Be(AclPrincipalType.Role);
        entry.PrincipalId.Should().Be(roleId);
        entry.ResourceType.Should().Be("Project");
        entry.ResourceId.Should().Be("proj-1");
        entry.AccessLevel.Should().Be(AccessLevel.Admin);
        entry.IsDenied.Should().BeFalse();
    }

    [Fact]
    public void ForRole_WithDeny_ShouldCreateDenyEntry()
    {
        var roleId = Guid.NewGuid();
        var entry = AccessControlListEntry.ForRole(
            _tenantId, roleId, "Project", "proj-1",
            AccessLevel.Admin, _grantedBy, isDenied: true);

        entry.IsDenied.Should().BeTrue();
    }

    [Fact]
    public void ForGroup_ShouldCreateGroupBasedEntry()
    {
        var groupId = Guid.NewGuid();
        var entry = AccessControlListEntry.ForGroup(
            _tenantId, groupId, "Document", "doc-1",
            AccessLevel.Read, _grantedBy);

        entry.PrincipalType.Should().Be(AclPrincipalType.Group);
        entry.PrincipalId.Should().Be(groupId);
        entry.ResourceType.Should().Be("Document");
        entry.ResourceId.Should().Be("doc-1");
        entry.AccessLevel.Should().Be(AccessLevel.Read);
    }

    [Fact]
    public void ForGroup_WithDeny_ShouldCreateDenyEntry()
    {
        var groupId = Guid.NewGuid();
        var entry = AccessControlListEntry.ForGroup(
            _tenantId, groupId, "Document", "doc-1",
            AccessLevel.Read, _grantedBy, isDenied: true);

        entry.IsDenied.Should().BeTrue();
    }

    [Fact]
    public void ForAnonymous_ShouldCreateAnonymousEntry()
    {
        var entry = AccessControlListEntry.ForAnonymous(
            _tenantId, "Page", "page-1",
            AccessLevel.Read, _grantedBy);

        entry.PrincipalType.Should().Be(AclPrincipalType.Anonymous);
        entry.PrincipalId.Should().BeNull();
        entry.ResourceType.Should().Be("Page");
        entry.ResourceId.Should().Be("page-1");
        entry.AccessLevel.Should().Be(AccessLevel.Read);
        entry.IsDenied.Should().BeFalse();
    }

    [Fact]
    public void ForAnonymous_WithDeny_ShouldCreateDenyEntry()
    {
        var entry = AccessControlListEntry.ForAnonymous(
            _tenantId, "Page", "page-1",
            AccessLevel.Write, _grantedBy, isDenied: true);

        entry.IsDenied.Should().BeTrue();
        entry.PrincipalType.Should().Be(AclPrincipalType.Anonymous);
    }

    [Fact]
    public void ForUser_ShouldSetGrantedAt()
    {
        var before = DateTime.UtcNow;

        var entry = AccessControlListEntry.ForUser(
            _tenantId, _userId, "Course", "c-1",
            AccessLevel.Read, _grantedBy);

        entry.GrantedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void ForRole_ShouldSetGrantedAt()
    {
        var before = DateTime.UtcNow;

        var entry = AccessControlListEntry.ForRole(
            _tenantId, Guid.NewGuid(), "Course", "c-1",
            AccessLevel.Read, _grantedBy);

        entry.GrantedAt.Should().BeOnOrAfter(before);
    }
}
