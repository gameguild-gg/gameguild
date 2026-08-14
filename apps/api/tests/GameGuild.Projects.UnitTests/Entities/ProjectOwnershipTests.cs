namespace GameGuild.Projects.UnitTests.Entities;

public sealed class ProjectOwnershipTests
{
    [Fact]
    public void SetOwnerTeam_Should_Keep_Exactly_One_Owner()
    {
        var project = new Project();
        var firstOwner = Guid.NewGuid();
        var replacementOwner = Guid.NewGuid();

        project.SetOwnerTeam(firstOwner);
        project.SetOwnerTeam(replacementOwner);

        project.Teams.Should().ContainSingle(team => team.Role == ProjectTeamRole.Owner && team.IsActive);
        project.Teams.Single(team => team.TeamId == firstOwner).Role.Should().Be(ProjectTeamRole.CoOwner);
        project.Teams.Single(team => team.TeamId == replacementOwner).ParticipationMode.Should().Be(ProjectTeamParticipationMode.AllMembers);
    }

    [Fact]
    public void AddParticipatingTeam_Should_Default_To_SelectedMembers()
    {
        var project = new Project();

        var projectTeam = project.AddParticipatingTeam(Guid.NewGuid(), ProjectTeamRole.Contributor);

        projectTeam.ParticipationMode.Should().Be(ProjectTeamParticipationMode.SelectedMembers);
    }

    [Fact]
    public void AddAllocation_Should_Reject_Capacity_Outside_Allowed_Range()
    {
        var project = new Project();
        var projectTeam = project.AddParticipatingTeam(Guid.NewGuid(), ProjectTeamRole.Contributor);

        var action = () => project.AddAllocation(projectTeam.Id, Guid.NewGuid(), "Developer", 101);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Agreement_Should_Require_Acceptance_By_Two_Distinct_Actors()
    {
        var proposerId = Guid.NewGuid();
        var agreement = ProjectTeamAgreement.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            proposerId,
            "Prototype delivery",
            "Playable build",
            SystemClock.UtcNow,
            SystemClock.UtcNow.AddMonths(1));

        var sameActor = () => agreement.Accept(proposerId);
        sameActor.Should().Throw<InvalidOperationException>().WithMessage("*distinct actor*");

        agreement.Accept(Guid.NewGuid());
        agreement.Status.Should().Be(ProjectTeamAgreementStatus.Accepted);
    }
}
