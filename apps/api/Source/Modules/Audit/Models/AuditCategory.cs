namespace GameGuild.Modules.Audit;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users;

/// <summary>
/// Category of audit events
/// </summary>
public enum AuditCategory
{
    General = 0,

    Authentication = 1,

    Authorization = 2,

    Permission = 3,

    User = 4,

    Admin = 5,

    Security = 6,

    Data = 7,

    System = 8,

    Tenant = 9,

    Privacy = 10,
}
