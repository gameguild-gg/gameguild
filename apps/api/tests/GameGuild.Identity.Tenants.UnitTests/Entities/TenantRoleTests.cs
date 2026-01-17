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
        TenantRole.Moderator.CanManageContent.Should().BeTrue();
        TenantRole.Member.CanCreateContent.Should().BeTrue();
        TenantRole.Viewer.CanCreateContent.Should().BeFalse();
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
}
