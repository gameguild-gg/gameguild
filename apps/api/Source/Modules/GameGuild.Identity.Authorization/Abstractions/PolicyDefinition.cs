namespace GameGuild.Identity.Authorization;

/// <summary>
///     Represents a cached authorization policy definition.
/// </summary>
public sealed class PolicyDefinition
{
    /// <summary>
    ///     Gets or sets the unique name of the policy.
    /// </summary>
    public required string PolicyName { get; init; }

    /// <summary>
    ///     Gets or sets whether the policy requires an authenticated user.
    /// </summary>
    public bool RequireAuthentication { get; init; } = true;

    /// <summary>
    ///     Gets or sets the authentication schemes required by this policy.
    /// </summary>
    public IReadOnlyList<string> AuthenticationSchemes { get; init; } = [];

    /// <summary>
    ///     Gets or sets the permissions required by this policy (RBAC).
    /// </summary>
    public IReadOnlyList<string> RequiredPermissions { get; init; } = [];

    /// <summary>
    ///     Gets or sets the roles required by this policy.
    /// </summary>
    public IReadOnlyList<string> RequiredRoles { get; init; } = [];

    /// <summary>
    ///     Gets or sets whether Access Control List-based access should be checked (DAC).
    /// </summary>
    public bool RequireAccessControlListAccess { get; init; }

    /// <summary>
    ///     Gets or sets the resource type for DAC checks.
    /// </summary>
    public string? ResourceType { get; init; }

    /// <summary>
    ///     Gets or sets the minimum access level required.
    /// </summary>
    public string? MinimumAccessLevel { get; init; }

    /// <summary>
    ///     Gets or sets whether this policy is tenant-scoped.
    /// </summary>
    public bool IsTenantScoped { get; init; }

    /// <summary>
    ///     Gets or sets the version for cache invalidation.
    /// </summary>
    public long Version { get; init; }

    /// <summary>
    ///     Gets or sets the rules that make up this policy.
    /// </summary>
    public IReadOnlyList<PolicyRule>? Rules { get; init; }

    /// <summary>
    ///     Gets or sets whether to use the new rule-based evaluation.
    /// </summary>
    public bool UseRuleBasedEvaluation { get; init; }

    public bool IsConfigurationValid { get; init; } = true;
}

/// <summary>
///     Represents a single rule in a policy ruleset.
/// </summary>
public sealed record PolicyRule
{
    /// <summary>
    ///     The type of rule (e.g., "TenantMatch", "RequireAllPermissions").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    ///     Human-readable description of this rule.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     Parameters for the rule evaluation.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Params { get; init; }

    public IReadOnlyList<PolicyRule>? Rules { get; init; }

    /// <summary>
    ///     Whether this rule is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;
}

/// <summary>
///     ABAC environment constraints for policy evaluation.
/// </summary>
public sealed class EnvironmentConstraints
{
    /// <summary>
    ///     Allowed IP ranges (CIDR notation).
    /// </summary>
    public IReadOnlyList<string> AllowedIpRanges { get; init; } = [];

    /// <summary>
    ///     Allowed time windows with optional timezone support.
    ///     Accepts both string format ("HH:mm-HH:mm" or "HH:mm-HH:mm@TimeZoneId")
    ///     and <see cref="TimeWindow"/> objects.
    /// </summary>
    public IReadOnlyList<TimeWindow> AllowedTimeWindows { get; init; } = [];

    /// <summary>
    ///     Required device types (e.g., "mobile", "desktop").
    /// </summary>
    public IReadOnlyList<string> RequiredDeviceTypes { get; init; } = [];

    /// <summary>
    ///     Blocked geographic regions.
    /// </summary>
    public IReadOnlyList<string> BlockedRegions { get; init; } = [];

    /// <summary>
    ///     Whether secure connection (HTTPS) is required.
    /// </summary>
    public bool RequireSecureConnection { get; init; }
}
