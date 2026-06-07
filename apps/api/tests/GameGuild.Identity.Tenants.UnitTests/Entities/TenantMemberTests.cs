using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Entities;

public class TenantMemberTests
{
    [Fact]
    public void TenantMember_Partial_Constructor_Should_Map_Properties()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var member = new TenantMember(new { TenantId = tenantId, UserId = userId, Role = "Admin" });

        member.TenantId.Should().Be(tenantId);
        member.UserId.Should().Be(userId);
        member.Role.Should().Be("Admin");
    }

    [Fact]
    public void TenantMember_Should_Default_To_Active()
    {
        var member = new TenantMember
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = "Member"
        };

        member.IsActive.Should().BeTrue();
        member.JoinedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Activate_Should_Reset_LeftAt_And_LeaveReason()
    {
        var member = new TenantMember
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = "Member",
            IsActive = false,
            LeftAt = DateTime.UtcNow.AddDays(-1),
            LeaveReason = "Left"
        };

        member.Activate();

        member.IsActive.Should().BeTrue();
        member.LeftAt.Should().BeNull();
        member.LeaveReason.Should().BeNull();
    }

    [Fact]
    public void Deactivate_Should_Set_LeftAt_And_Reason()
    {
        var member = new TenantMember
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = "Member"
        };

        member.Deactivate("Violation");

        member.IsActive.Should().BeFalse();
        member.LeftAt.Should().NotBeNull();
        member.LeaveReason.Should().Be("Violation");
    }

    [Fact]
    public void UpdateRole_Should_Change_Role()
    {
        var member = new TenantMember
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = "Member"
        };

        member.UpdateRole("Admin");

        member.Role.Should().Be("Admin");
    }

    [Fact]
    public void SetParent_Should_Update_ParentMemberId()
    {
        var member = new TenantMember
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = "Member"
        };

        var parentId = Guid.NewGuid();
        member.SetParent(parentId);

        member.ParentMemberId.Should().Be(parentId);

        member.SetParent(null);
        member.ParentMemberId.Should().BeNull();
    }
}
