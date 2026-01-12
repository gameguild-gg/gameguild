using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Requirement that validates a user has a specific permission (RBAC).
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    ///     Gets the required permission name.
    /// </summary>
    public string Permission { get; }

    /// <summary>
    ///     Gets whether permission can be satisfied by claims or requires database lookup.
    /// </summary>
    public bool AllowClaimsBased { get; }

    /// <summary>
    ///     Initializes a new instance of <see cref="PermissionRequirement"/>.
    /// </summary>
    /// <param name="permission">The required permission.</param>
    /// <param name="allowClaimsBased">If true, allows checking token claims before database.</param>
    public PermissionRequirement(string permission, bool allowClaimsBased = true)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
        AllowClaimsBased = allowClaimsBased;
    }
}
