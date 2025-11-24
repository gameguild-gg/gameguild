namespace GameGuild.Authentication.Entities;

/// <summary>
///     Access review scope
/// </summary>
public enum AccessReviewScope
{
    /// <summary>
    ///     Review tenant-level permissions
    /// </summary>
    Tenant = 1,

    /// <summary>
    ///     Review content-type permissions
    /// </summary>
    ContentType = 2,

    /// <summary>
    ///     Review resource-specific permissions
    /// </summary>
    Resource = 3,

    /// <summary>
    ///     Review all permission levels
    /// </summary>
    All = 4
}
