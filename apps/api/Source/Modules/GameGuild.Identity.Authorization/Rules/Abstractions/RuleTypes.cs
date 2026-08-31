namespace GameGuild.Identity.Authorization;

/// <summary>
///     Centralized rule type constants for the rule-based authorization system.
///     These constants are used in rule definitions, evaluator registrations, and policy seeds.
/// </summary>
public static class RuleTypes
{
    /// <summary>
    ///     Ensures user belongs to the request tenant.
    /// </summary>
    public const string TenantMatch = "TenantMatch";

    /// <summary>
    ///     Requires ALL specified permissions (AND logic).
    /// </summary>
    public const string RequireAllPermissions = "RequireAllPermissions";

    /// <summary>
    ///     Requires ANY of the specified permissions (OR logic).
    /// </summary>
    public const string RequireAnyPermission = "RequireAnyPermission";

    /// <summary>
    ///     Allows action if user is acting on themselves OR has a management permission.
    /// </summary>
    public const string SelfOrPermission = "SelfOrPermission";

    /// <summary>
    ///     Checks resource ownership OR ACL access.
    /// </summary>
    public const string OwnerOrAcl = "OwnerOrAcl";

    /// <summary>
    ///     Requires request IP to be within allowed CIDR ranges.
    /// </summary>
    public const string RequireIpAllowList = "RequireIpAllowList";

    /// <summary>
    ///     Restricts access to specific time windows.
    /// </summary>
    public const string RequireTimeWindow = "RequireTimeWindow";

    /// <summary>
    ///     Requires the user to have completed MFA verification.
    /// </summary>
    public const string RequireMfa = "RequireMfa";

    public const string AnyOf = "AnyOf";

    public const string CourseContentAccess = "CourseContentAccess";

    /// <summary>
    ///     All known rule types for validation.
    /// </summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        TenantMatch,
        RequireAllPermissions,
        RequireAnyPermission,
        SelfOrPermission,
        OwnerOrAcl,
        RequireIpAllowList,
        RequireTimeWindow,
        RequireMfa,
        AnyOf,
        CourseContentAccess
    };

    /// <summary>
    ///     Validates that a rule type is known.
    /// </summary>
    /// <param name="ruleType">The rule type to validate.</param>
    /// <returns>True if the rule type is valid.</returns>
    public static bool IsValid(string? ruleType) =>
        !string.IsNullOrWhiteSpace(ruleType) && All.Contains(ruleType);

    /// <summary>
    ///     Gets the required parameters for a rule type.
    /// </summary>
    /// <param name="ruleType">The rule type.</param>
    /// <returns>The required parameter names, or empty if none required.</returns>
#pragma warning disable IDE0072 // Populate switch - Explicit cases listed for documentation, default handles unlisted rule types
    public static IReadOnlyList<string> GetRequiredParameters(string ruleType) => ruleType switch
    {
        TenantMatch => [],
        RequireAllPermissions => ["permissions"],
        RequireAnyPermission => ["permissions"],
        SelfOrPermission => [],  // selfPermission or anyPermission recommended but optional
        OwnerOrAcl => [],
        RequireIpAllowList => ["cidrs"],
        RequireTimeWindow => ["windows"],
        RequireMfa => [],
        AnyOf => [],
        CourseContentAccess => ["access"],
        _ => []
    };
#pragma warning restore IDE0072

    /// <summary>
    ///     Gets a human-readable description for a rule type.
    /// </summary>
    /// <param name="ruleType">The rule type.</param>
    /// <returns>A description of the rule.</returns>
    public static string GetDescription(string ruleType) => ruleType switch
    {
        TenantMatch => "Ensures user belongs to the request tenant",
        RequireAllPermissions => "Requires ALL specified permissions",
        RequireAnyPermission => "Requires ANY of the specified permissions",
        SelfOrPermission => "Allows self-action or requires management permission",
        OwnerOrAcl => "Checks resource ownership or ACL access",
        RequireIpAllowList => "Requires IP to be in allowed ranges",
        RequireTimeWindow => "Restricts access to specific time windows",
        RequireMfa => "Requires MFA verification",
        AnyOf => "Requires at least one child rule to pass",
        CourseContentAccess => "Evaluates access to course content against the supplied course resource",
        _ => "Unknown rule type"
    };
}
