namespace GameGuild.Core.Domain.Permissions;

/// <summary> Source of permission grant/denial </summary>
public enum PermissionSource
{
    None = 0,

    GlobalDefault = 1,

    TenantDefault = 2,

    ContentTypeDefault = 3,

    TenantUser = 4,

    ContentTypeUser = 5,

    ResourceDefault = 6,

    ResourceUser = 7,

    SystemOverride = 8,
}
