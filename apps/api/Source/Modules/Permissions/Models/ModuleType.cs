namespace GameGuild.Modules.Permissions;

/// <summary> Module types that can have granular permissions </summary>
public enum ModuleType
{
    None = 0,

    TestingLab = 1,

    Projects = 2,

    Programs = 3,

    Courses = 4,

    Analytics = 5,

    UserManagement = 6,

    TenantManagement = 7,

    ContentManagement = 8,

    ApiManagement = 9,

    SystemAdministration = 10,
}