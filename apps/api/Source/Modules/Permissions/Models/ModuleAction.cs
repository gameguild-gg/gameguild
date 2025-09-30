namespace GameGuild.Modules.Permissions;

/// <summary> Actions that can be performed within a module </summary>
public enum ModuleAction
{
    None = 0,

    // Basic CRUD operations
    Create = 1,

    Read = 2,

    Edit = 3,

    Delete = 4,

    // Administrative operations
    Manage = 5,

    Administer = 6,

    // Specific operations
    Execute = 7,

    Review = 8,

    Approve = 9,

    Publish = 10,

    Archive = 11,

    Restore = 12,

    // Testing Lab specific
    CreateSession = 20,

    DeleteSession = 21,

    ManageTesters = 22,

    ViewReports = 23,

    ExportData = 24,

    // Project specific
    ManageCollaborators = 30,

    SetPermissions = 31,

    ManageReleases = 32,

    // System specific
    ManageUsers = 40,

    ManageRoles = 41,

    ViewAuditLogs = 42,

    SystemConfiguration = 43,
}
