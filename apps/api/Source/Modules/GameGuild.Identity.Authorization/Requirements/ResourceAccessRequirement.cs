using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Requirement that validates resource ownership and/or Access Control List-based access (DAC).
/// </summary>
public sealed class ResourceAccessRequirement : IAuthorizationRequirement
{
    /// <summary>
    ///     Gets whether resource ownership is required.
    /// </summary>
    public bool RequireOwnership { get; }

    /// <summary>
    ///     Gets whether Access Control List access check is required.
    /// </summary>
    public bool RequireAccessControlListAccess { get; }

    /// <summary>
    ///     Gets the minimum access level required when using Access Control List checks.
    /// </summary>
    public AccessLevel MinimumAccessLevel { get; }

    /// <summary>
    ///     Gets the resource type for Access Control List lookups.
    /// </summary>
    public string? ResourceType { get; }

    /// <summary>
    ///     Initializes a new instance of <see cref="ResourceAccessRequirement"/>.
    /// </summary>
    /// <param name="requireOwnership">Require resource ownership.</param>
    /// <param name="requireAccessControlListAccess">Require Access Control List-based access.</param>
    /// <param name="minimumAccessLevel">Minimum Access Control List access level.</param>
    /// <param name="resourceType">Resource type for Access Control List lookups.</param>
    public ResourceAccessRequirement(
        bool requireOwnership = false,
        bool requireAccessControlListAccess = false,
        AccessLevel minimumAccessLevel = AccessLevel.Read,
        string? resourceType = null)
    {
        RequireOwnership = requireOwnership;
        RequireAccessControlListAccess = requireAccessControlListAccess;
        MinimumAccessLevel = minimumAccessLevel;
        ResourceType = resourceType;
    }
}
