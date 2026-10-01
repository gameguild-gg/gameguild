namespace GameGuild.Configuration.PresentationLayer.Authorization;

/// <summary>
///     Declarative requirements for a host-registered ASP.NET Core authorization policy.
/// </summary>
public sealed class ConfiguredAuthorizationPolicyOptions
{
    /// <summary>
    ///     Whether the policy requires an authenticated principal. Defaults to true.
    /// </summary>
    public bool RequireAuthenticatedUser { get; set; } = true;

    /// <summary>
    ///     Roles accepted by the policy. Any listed role may satisfy this requirement.
    /// </summary>
    public List<string> Roles { get; set; } = [];

    /// <summary>
    ///     Claim requirements that must all be satisfied by the principal.
    /// </summary>
    public List<AuthorizationClaimRequirementOptions> Claims { get; set; } = [];

    /// <summary>
    ///     Authentication schemes that may authenticate the principal for this policy.
    /// </summary>
    public List<string> AuthenticationSchemes { get; set; } = [];
}

/// <summary>
///     A claim type and the optional set of accepted values for a configured policy.
/// </summary>
public sealed class AuthorizationClaimRequirementOptions
{
    /// <summary>The required claim type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    ///     Accepted values. An empty list requires the claim to be present with any value.
    /// </summary>
    public List<string> AllowedValues { get; set; } = [];
}
