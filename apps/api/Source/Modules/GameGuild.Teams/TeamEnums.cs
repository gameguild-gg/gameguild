namespace GameGuild.Teams;

public enum TeamVisibility
{
    Private = 0,
    Tenant = 1,
    Public = 2
}

public enum TeamStatus
{
    Active = 0,
    Archived = 1
}

public enum TeamMemberAuthority
{
    Viewer = 0,
    Member = 1,
    Manager = 2,
    Owner = 3
}
