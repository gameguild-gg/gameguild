namespace GameGuild.Identity.Authorization;

/// <summary>
///     Defines access levels for Access Control List-based access control.
/// </summary>
public enum AccessLevel
{
    /// <summary>
    ///     No access.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Read-only access.
    /// </summary>
    Read = 1,

    /// <summary>
    ///     Read and write access.
    /// </summary>
    Write = 2,

    /// <summary>
    ///     Full admin access including delete and sharing.
    /// </summary>
    Admin = 3
}

/// <summary>
///     Marker interface for resources that support ownership-based access.
/// </summary>
public interface IOwnedResource
{
    /// <summary>
    ///     Gets the owner's user ID.
    /// </summary>
    Guid OwnerId { get; }

    /// <summary>
    ///     Gets the tenant ID the resource belongs to.
    /// </summary>
    Guid TenantId { get; }
}

/// <summary>
///     Marker interface for resources that support tenant-scoped access.
/// </summary>
public interface ITenantResource
{
    /// <summary>
    ///     Gets the tenant ID the resource belongs to.
    /// </summary>
    Guid TenantId { get; }
}

/// <summary>
///     Marker interface for resources that support Access Control List-based access.
/// </summary>
public interface IAccessControlListResource : IOwnedResource
{
    /// <summary>
    ///     Gets the resource type identifier for Access Control List lookups.
    /// </summary>
    string ResourceType { get; }

    /// <summary>
    ///     Gets the unique resource identifier for Access Control List lookups.
    /// </summary>
    string ResourceId { get; }
}
