namespace GameGuild.Modules.Permissions;

/// <summary> Predefined roles for Testing Lab module </summary>
public enum TestingLabRole
{
    None = 0,

    Admin = 1, // Full access to everything

    Manager = 2, // Can create/edit but not delete sessions, manage testers

    Coordinator = 3, // Can create sessions and manage testers 

    Tester = 4, // Can only participate in sessions
}