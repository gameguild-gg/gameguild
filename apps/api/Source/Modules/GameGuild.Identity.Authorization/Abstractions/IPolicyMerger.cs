using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Merges base tenant policies with tenant-specific overrides.
/// </summary>
public interface IPolicyMerger
{
    /// <summary>
    ///     Merges a base policy definition with a tenant-specific override.
    /// </summary>
    /// <param name="basePolicy">The base policy definition.</param>
    /// <param name="tenantOverride">The tenant-specific override (can be null).</param>
    /// <returns>The merged policy definition.</returns>
    PolicyDefinition Merge(PolicyDefinition basePolicy, PolicyDefinition? tenantOverride);

    /// <summary>
    ///     Builds an AuthorizationPolicy from a PolicyDefinition.
    /// </summary>
    /// <param name="definition">The policy definition.</param>
    /// <returns>The compiled authorization policy.</returns>
    AuthorizationPolicy Build(PolicyDefinition definition);
}
