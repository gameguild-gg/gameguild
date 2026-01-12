using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Requirement that validates the token's tenant claim matches the resolved tenant context.
/// </summary>
public sealed class TenantMatchRequirement : IAuthorizationRequirement
{
    /// <summary>
    ///     Gets whether strict matching is required (no fallback to base tenant).
    /// </summary>
    public bool StrictMatch { get; }

    /// <summary>
    ///     Initializes a new instance of <see cref="TenantMatchRequirement"/>.
    /// </summary>
    /// <param name="strictMatch">If true, requires exact tenant match with no fallbacks.</param>
    public TenantMatchRequirement(bool strictMatch = false)
    {
        StrictMatch = strictMatch;
    }
}
