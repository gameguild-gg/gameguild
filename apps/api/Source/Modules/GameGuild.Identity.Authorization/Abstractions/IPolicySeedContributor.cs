namespace GameGuild.Identity.Authorization;

/// <summary>
///     Extension point for policy seeds that are specific to a product or domain.
/// </summary>
/// <remarks>
///     <para>
///         The common <see cref="PolicyDefinitionSeeder"/> seeds only platform-generic
///         policies. Anything domain-specific (product roles, role-admission gates, custom
///         resource policies) is supplied by contributors registered from the host or a
///         domain module via <c>AddPolicySeedContributor&lt;T&gt;()</c> — keeping the
///         common module free of domain identifiers.
///     </para>
///     <para>
///         Contributor policies follow the same seeding semantics as platform policies:
///         missing definitions are created; existing definitions are refreshed from the
///         canonical entity when their stored <see cref="PolicyDefinitionEntity.PolicyVersion"/>
///         is older than the seeder's current version.
///     </para>
/// </remarks>
public interface IPolicySeedContributor
{
    /// <summary>
    ///     Short identity of the contributor, used for seeding diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Builds the canonical policy definitions this contributor contributes.
    /// </summary>
    /// <returns>The policies to seed.</returns>
    IEnumerable<PolicyDefinitionEntity> BuildPolicies();
}
