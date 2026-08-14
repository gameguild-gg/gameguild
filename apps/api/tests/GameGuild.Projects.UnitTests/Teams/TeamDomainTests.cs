using GameGuild.Teams;

namespace GameGuild.Projects.UnitTests.Teams;

public sealed class TeamDomainTests
{
    [Fact]
    public void CreatePersonalTeam_Should_Create_Exactly_One_Owner()
    {
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var team = Team.Create(tenantId, "Matheus Martins", "matheus-martins", ownerId, isPersonal: true);

        team.TenantId.Should().Be(tenantId);
        team.IsPersonal.Should().BeTrue();
        team.Members.Should().ContainSingle(member =>
            member.UserId == ownerId && member.Authority == TeamMemberAuthority.Owner && member.IsActive);
    }

    [Fact]
    public void Member_Authority_Should_Be_Independent_From_Professional_Title()
    {
        var team = Team.Create(Guid.NewGuid(), "Studio", "studio", Guid.NewGuid());
        var member = team.AddMember(Guid.NewGuid(), TeamMemberAuthority.Member, "Gameplay Programmer");

        member.Authority.Should().Be(TeamMemberAuthority.Member);
        member.ProfessionalTitle.Should().Be("Gameplay Programmer");
    }

    [Fact]
    public void RemoveMember_Should_Reject_Removing_The_Last_Active_Owner()
    {
        var ownerId = Guid.NewGuid();
        var team = Team.Create(Guid.NewGuid(), "Studio", "studio", ownerId);

        var action = () => team.RemoveMember(ownerId);

        action.Should().Throw<InvalidOperationException>().WithMessage("*last active owner*");
    }

    [Fact]
    public void Invitation_Should_Hash_Token_Expire_And_Be_Use_Once()
    {
        const string token = "secret-team-invitation";
        var now = new DateTime(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);
        var invitation = TeamInvitation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "member@example.com",
            TeamMemberAuthority.Member,
            token,
            now.AddHours(1));

        invitation.TokenHash.Should().NotBe(token);
        invitation.Accept(token, Guid.NewGuid(), now.AddMinutes(1)).Should().BeTrue();
        invitation.Accept(token, Guid.NewGuid(), now.AddMinutes(2)).Should().BeFalse();

        var expired = TeamInvitation.Create(
            invitation.TenantId!.Value,
            invitation.TeamId,
            invitation.InvitedByUserId,
            "late@example.com",
            TeamMemberAuthority.Viewer,
            "another-token",
            now.AddMinutes(5));

        expired.Accept("another-token", Guid.NewGuid(), now.AddMinutes(6)).Should().BeFalse();
    }
}
