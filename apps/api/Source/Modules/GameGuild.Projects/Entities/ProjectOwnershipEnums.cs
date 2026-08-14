namespace GameGuild.Projects;

public enum ProjectTeamRole
{
    Owner = 0,
    CoOwner = 1,
    Contributor = 2,
    Guest = 3
}

public enum ProjectTeamParticipationMode
{
    AllMembers = 0,
    SelectedMembers = 1
}

public enum ProjectTeamAgreementStatus
{
    Proposed = 0,
    CounterProposed = 1,
    Accepted = 2,
    Cancelled = 3,
    Completed = 4
}
