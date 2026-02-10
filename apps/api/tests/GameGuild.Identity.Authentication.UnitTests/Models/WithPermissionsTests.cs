using FluentAssertions;
using GameGuild.Identity.Authorization;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Models;

public class WithPermissionsTests
{
    // Use ContentTypePermission as a concrete test subject
    private ContentTypePermission CreatePermission(Guid? userId = null, Guid? tenantId = null)
    {
        return new ContentTypePermission(userId, tenantId, "TestContent");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var perm = CreatePermission();

        perm.Permissions.Should().BeEmpty();
        perm.IsActive.Should().BeTrue();
        perm.ExpiresAt.Should().BeNull();
        perm.Notes.Should().BeNull();
        perm.GrantedBy.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithUserIdAndTenantId_ShouldSetProperties()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var perm = CreatePermission(userId, tenantId);

        perm.UserId.Should().Be(userId);
    }

    [Fact]
    public void Constructor_WithNullUserId_ShouldSetNull()
    {
        var perm = CreatePermission(null, Guid.NewGuid());

        perm.UserId.Should().BeNull();
    }

    [Fact]
    public void AddPermission_ShouldAddToPermissions()
    {
        var perm = CreatePermission();

        perm.AddPermission(PermissionType.Read);

        perm.HasPermission(PermissionType.Read).Should().BeTrue();
    }

    [Fact]
    public void AddPermission_DuplicatePermission_ShouldNotAddTwice()
    {
        var perm = CreatePermission();

        perm.AddPermission(PermissionType.Read);
        perm.AddPermission(PermissionType.Read);

        perm.GetPermissionsAsEnum().Count().Should().Be(1);
    }

    [Fact]
    public void AddPermission_MultiplePermissions_ShouldAddAll()
    {
        var perm = CreatePermission();

        perm.AddPermission(PermissionType.Read);
        perm.AddPermission(PermissionType.Comment);
        perm.AddPermission(PermissionType.Vote);

        perm.GetPermissionsAsEnum().Count().Should().Be(3);
        perm.HasPermission(PermissionType.Read).Should().BeTrue();
        perm.HasPermission(PermissionType.Comment).Should().BeTrue();
        perm.HasPermission(PermissionType.Vote).Should().BeTrue();
    }

    [Fact]
    public void RemovePermission_ShouldRemoveFromPermissions()
    {
        var perm = CreatePermission();
        perm.AddPermission(PermissionType.Read);
        perm.AddPermission(PermissionType.Comment);

        perm.RemovePermission(PermissionType.Read);

        perm.HasPermission(PermissionType.Read).Should().BeFalse();
        perm.HasPermission(PermissionType.Comment).Should().BeTrue();
    }

    [Fact]
    public void RemovePermission_WhenNotPresent_ShouldNotThrow()
    {
        var perm = CreatePermission();

        perm.RemovePermission(PermissionType.Read);

        perm.GetPermissionsAsEnum().Should().BeEmpty();
    }

    [Fact]
    public void HasPermission_WhenNotPresent_ShouldReturnFalse()
    {
        var perm = CreatePermission();

        perm.HasPermission(PermissionType.Read).Should().BeFalse();
    }

    [Fact]
    public void GetPermissionsAsEnum_WhenEmpty_ShouldReturnEmpty()
    {
        var perm = CreatePermission();

        perm.GetPermissionsAsEnum().Should().BeEmpty();
    }

    [Fact]
    public void GetPermissionsAsEnum_WhenWhitespace_ShouldReturnEmpty()
    {
        var perm = CreatePermission();
        perm.Permissions = "   ";

        perm.GetPermissionsAsEnum().Should().BeEmpty();
    }

    [Fact]
    public void GetPermissionsAsEnum_ShouldParseCorrectly()
    {
        var perm = CreatePermission();
        perm.Permissions = "1,2,3";

        var result = perm.GetPermissionsAsEnum().ToList();

        result.Should().HaveCount(3);
        result.Should().Contain(PermissionType.Read);
        result.Should().Contain(PermissionType.Comment);
        result.Should().Contain(PermissionType.Reply);
    }

    [Fact]
    public void GetPermissionsAsEnum_WithInvalidData_ShouldSkipInvalid()
    {
        var perm = CreatePermission();
        perm.Permissions = "1,abc,3";

        var result = perm.GetPermissionsAsEnum().ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(PermissionType.Read);
        result.Should().Contain(PermissionType.Reply);
    }

    [Fact]
    public void SetPermissions_ShouldReplaceAllPermissions()
    {
        var perm = CreatePermission();
        perm.AddPermission(PermissionType.Read);

        perm.SetPermissions(new[] { PermissionType.Comment, PermissionType.Vote });

        perm.HasPermission(PermissionType.Read).Should().BeFalse();
        perm.HasPermission(PermissionType.Comment).Should().BeTrue();
        perm.HasPermission(PermissionType.Vote).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtNull_ShouldReturnFalse()
    {
        var perm = CreatePermission();
        perm.ExpiresAt = null;

        perm.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInFuture_ShouldReturnFalse()
    {
        var perm = CreatePermission();
        perm.ExpiresAt = DateTime.UtcNow.AddDays(1);

        perm.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInPast_ShouldReturnTrue()
    {
        var perm = CreatePermission();
        perm.ExpiresAt = DateTime.UtcNow.AddDays(-1);

        perm.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsEffective_WhenActiveAndNotExpired_ShouldReturnTrue()
    {
        var perm = CreatePermission();
        perm.IsActive = true;
        perm.ExpiresAt = null;

        perm.IsEffective().Should().BeTrue();
    }

    [Fact]
    public void IsEffective_WhenInactive_ShouldReturnFalse()
    {
        var perm = CreatePermission();
        perm.IsActive = false;

        perm.IsEffective().Should().BeFalse();
    }

    [Fact]
    public void IsEffective_WhenExpired_ShouldReturnFalse()
    {
        var perm = CreatePermission();
        perm.IsActive = true;
        perm.ExpiresAt = DateTime.UtcNow.AddDays(-1);

        perm.IsEffective().Should().BeFalse();
    }

    [Fact]
    public void Expire_ShouldDeactivateAndSetExpiry()
    {
        var perm = CreatePermission();
        perm.IsActive = true;

        perm.Expire();

        perm.IsActive.Should().BeFalse();
        perm.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public void ExtendExpiration_ShouldUpdateExpiresAt()
    {
        var perm = CreatePermission();
        var newDate = DateTime.UtcNow.AddDays(30);

        perm.ExtendExpiration(newDate);

        perm.ExpiresAt.Should().Be(newDate);
    }

    [Fact]
    public void ExtendExpiration_WithNull_ShouldMakePermanent()
    {
        var perm = CreatePermission();
        perm.ExpiresAt = DateTime.UtcNow.AddDays(1);

        perm.ExtendExpiration(null);

        perm.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void AddPermissions_Bulk_ShouldAddNewOnly()
    {
        var perm = CreatePermission();
        perm.AddPermission(PermissionType.Read);

        perm.AddPermissions(new[] { PermissionType.Read, PermissionType.Comment, PermissionType.Vote });

        var result = perm.GetPermissionsAsEnum().ToList();
        result.Should().HaveCount(3);
    }

    [Fact]
    public void AddPermissions_WhenAllExist_ShouldNotChange()
    {
        var perm = CreatePermission();
        perm.AddPermission(PermissionType.Read);
        perm.AddPermission(PermissionType.Comment);

        perm.AddPermissions(new[] { PermissionType.Read, PermissionType.Comment });

        perm.GetPermissionsAsEnum().Count().Should().Be(2);
    }

    [Fact]
    public void RemovePermissions_Bulk_ShouldRemoveSpecified()
    {
        var perm = CreatePermission();
        perm.AddPermission(PermissionType.Read);
        perm.AddPermission(PermissionType.Comment);
        perm.AddPermission(PermissionType.Vote);

        perm.RemovePermissions(new[] { PermissionType.Read, PermissionType.Vote });

        perm.HasPermission(PermissionType.Read).Should().BeFalse();
        perm.HasPermission(PermissionType.Vote).Should().BeFalse();
        perm.HasPermission(PermissionType.Comment).Should().BeTrue();
    }

    [Fact]
    public void RemovePermissions_WhenNoneExist_ShouldNotChange()
    {
        var perm = CreatePermission();
        perm.AddPermission(PermissionType.Read);

        perm.RemovePermissions(new[] { PermissionType.Comment, PermissionType.Vote });

        perm.GetPermissionsAsEnum().Count().Should().Be(1);
        perm.HasPermission(PermissionType.Read).Should().BeTrue();
    }
}

public class ContentTypePermissionTests
{
    [Fact]
    public void DefaultConstructor_ShouldSetDefaults()
    {
        var perm = new ContentTypePermission();

        perm.ContentTypeName.Should().BeEmpty();
        perm.Description.Should().BeNull();
        perm.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithValidArgs_ShouldSetProperties()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var perm = new ContentTypePermission(userId, tenantId, "Document");

        perm.UserId.Should().Be(userId);
        perm.ContentTypeName.Should().Be("Document");
    }

    [Fact]
    public void Constructor_WithNullContentTypeName_ShouldThrow()
    {
        var act = () => new ContentTypePermission(Guid.NewGuid(), Guid.NewGuid(), null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("contentTypeName");
    }

    [Fact]
    public void IsDefaultPermission_WhenNoUserId_ShouldReturnTrue()
    {
        var perm = new ContentTypePermission(null, Guid.NewGuid(), "Course");

        perm.IsDefaultPermission().Should().BeTrue();
    }

    [Fact]
    public void IsDefaultPermission_WhenHasUserId_ShouldReturnFalse()
    {
        var perm = new ContentTypePermission(Guid.NewGuid(), Guid.NewGuid(), "Course");

        perm.IsDefaultPermission().Should().BeFalse();
    }

    [Fact]
    public void IsUserSpecificPermission_WhenHasUserId_ShouldReturnTrue()
    {
        var perm = new ContentTypePermission(Guid.NewGuid(), Guid.NewGuid(), "Course");

        perm.IsUserSpecificPermission().Should().BeTrue();
    }

    [Fact]
    public void IsUserSpecificPermission_WhenNoUserId_ShouldReturnFalse()
    {
        var perm = new ContentTypePermission(null, Guid.NewGuid(), "Course");

        perm.IsUserSpecificPermission().Should().BeFalse();
    }

    [Fact]
    public void UpdateContentTypeName_WithValidName_ShouldUpdate()
    {
        var perm = new ContentTypePermission(null, Guid.NewGuid(), "OldName");

        perm.UpdateContentTypeName("NewName");

        perm.ContentTypeName.Should().Be("NewName");
    }

    [Fact]
    public void UpdateContentTypeName_WithNullOrWhitespace_ShouldThrow()
    {
        var perm = new ContentTypePermission(null, Guid.NewGuid(), "Test");

        var act1 = () => perm.UpdateContentTypeName(null!);
        var act2 = () => perm.UpdateContentTypeName("");
        var act3 = () => perm.UpdateContentTypeName("   ");

        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDescription_ShouldUpdateDescription()
    {
        var perm = new ContentTypePermission(null, Guid.NewGuid(), "Test");

        perm.UpdateDescription("New description");

        perm.Description.Should().Be("New description");
    }

    [Fact]
    public void UpdateDescription_WithNull_ShouldSet()
    {
        var perm = new ContentTypePermission(null, Guid.NewGuid(), "Test");
        perm.Description = "existing";

        perm.UpdateDescription(null);

        perm.Description.Should().BeNull();
    }
}
