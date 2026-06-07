using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Entities;

public class TenantRoleTests
{
    [Fact]
    public void Role_Permissions_Should_Be_Correct()
    {
        TenantRole.Owner.IsAdmin.Should().BeTrue();
        TenantRole.Admin.IsAdmin.Should().BeTrue();
        TenantRole.Moderator.IsAdmin.Should().BeFalse();
        TenantRole.Admin.CanManageContent.Should().BeTrue();
        TenantRole.Moderator.CanManageContent.Should().BeTrue();
        TenantRole.Member.CanCreateContent.Should().BeTrue();
        TenantRole.Viewer.CanCreateContent.Should().BeFalse();
    }

    [Fact]
    public void Role_Collections_And_Known_Role_Checks_Should_Be_Correct()
    {
        TenantRole.AdminRoles.Should().Equal(TenantRole.Owner, TenantRole.Admin);
        TenantRole.ContentManagerRoles.Should().Equal(TenantRole.Owner, TenantRole.Admin, TenantRole.Moderator);
        TenantRole.IsKnownRole("admin").Should().BeTrue();
        TenantRole.IsKnownRole("custom").Should().BeFalse();
        TenantRole.Admin.ToString().Should().Be(TenantRole.Admin.Value);
    }

    [Fact]
    public void FromString_Should_Return_Known_Or_Custom()
    {
        var known = TenantRole.FromString("admin");
        var custom = TenantRole.FromString("Custom");

        known.Should().Be(TenantRole.Admin);
        custom.Value.Should().Be("Custom");
    }

    [Fact]
    public void TryParse_Should_Identify_Known_Role()
    {
        TenantRole.TryParse("member", out var role).Should().BeTrue();
        role.Should().Be(TenantRole.Member);
        TenantRole.TryParse("unknown", out var unknown).Should().BeFalse();
        unknown.Should().BeNull();
    }

    [Fact]
    public void Equality_Should_Work_With_String_And_Role()
    {
        (TenantRole.Admin == "admin").Should().BeTrue();
        (TenantRole.Admin != "member").Should().BeTrue();
        TenantRole.Admin.Equals("ADMIN").Should().BeTrue();
        TenantRole.Admin.Equals(TenantRole.Admin).Should().BeTrue();
    }

    [Fact]
    public void Equality_Should_Handle_Nulls_And_Unrelated_Objects()
    {
        TenantRole? noRole = null;
        var admin = TenantRole.Admin;
        object unrelated = new();
        var adminUpper = TenantRole.FromString("ADMIN");
        ArgumentNullException.ThrowIfNull(admin);
        ArgumentNullException.ThrowIfNull(adminUpper);
        var bothRolesNull = noRole == (TenantRole?)null;
        var differsFromAdmin = noRole != admin;
        var matchesNullString = noRole == (string?)null;
        var matchesAdminString = noRole == "admin";
        var adminEqualsNullString = admin == (string?)null;
        var equalsUnrelated = object.Equals(admin, unrelated);
    #pragma warning disable CS8602
        var hashCodesMatch = admin.GetHashCode() == adminUpper.GetHashCode();
    #pragma warning restore CS8602

        bothRolesNull.Should().BeTrue();
        differsFromAdmin.Should().BeTrue();
        matchesNullString.Should().BeTrue();
        matchesAdminString.Should().BeFalse();
        adminEqualsNullString.Should().BeFalse();
        equalsUnrelated.Should().BeFalse();
        hashCodesMatch.Should().BeTrue();
    }
}
