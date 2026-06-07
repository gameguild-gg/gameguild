namespace GameGuild.Commerce.Payments;

/// <summary>Audit action types</summary>
public enum AuditAction
{
    /// <summary>Created</summary>
    Created = 0,

    /// <summary>Updated</summary>
    Updated = 1,

    /// <summary>Deleted</summary>
    Deleted = 2,

    /// <summary>Restored</summary>
    Restored = 3,

    /// <summary>Status changed</summary>
    StatusChanged = 4,

    /// <summary>Permission changed</summary>
    PermissionChanged = 5,

    /// <summary>Configuration changed</summary>
    ConfigurationChanged = 6,

    /// <summary>Other</summary>
    Other = 7
}
