using GameGuild.Database;
using GameGuild.Modules.Permissions.Constants;

namespace GameGuild.Modules.Permissions.Entities;

/// <summary>
/// Represents a conditional policy that enforces permission rules based on time, environment, or other contextual factors
/// </summary>
public class ConditionalPolicy : EntityBase<Guid>
{
    /// <summary>
    /// Tenant ID to which this policy belongs (null for global policies)
    /// </summary>
    public override Guid? TenantId { get; set; }

    /// <summary>
    /// Name of the conditional policy
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this policy enforces
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of condition (Time, Environment, Location, etc.)
    /// </summary>
    public PolicyConditionType ConditionType { get; set; }

    /// <summary>
    /// Permission type this policy applies to (null means all permissions)
    /// </summary>
    public PermissionType? PermissionType { get; set; }

    /// <summary>
    /// Target resource type (null means all resource types)
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// Action to take when condition matches (Allow, Deny, Require2FA)
    /// </summary>
    public PolicyAction Action { get; set; }

    /// <summary>
    /// Priority for policy evaluation (higher = evaluated first)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Whether this policy is currently active
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Time-based conditions (JSON serialized)
    /// Contains: DaysOfWeek, TimeRanges (StartTime, EndTime), TimeZone
    /// </summary>
    public string? TimeConditions { get; set; }

    /// <summary>
    /// Environment conditions (JSON serialized)
    /// Contains: Environments (Production, Staging, Development), IpRanges, Countries
    /// </summary>
    public string? EnvironmentConditions { get; set; }

    /// <summary>
    /// Location conditions (JSON serialized)
    /// Contains: AllowedCountries, DeniedCountries, AllowedRegions, DeniedRegions
    /// </summary>
    public string? LocationConditions { get; set; }

    /// <summary>
    /// Device conditions (JSON serialized)
    /// Contains: AllowedDeviceTypes, RequireCompliancy, RequireEncryption
    /// </summary>
    public string? DeviceConditions { get; set; }

    /// <summary>
    /// Custom conditions (JSON serialized) for extensibility
    /// </summary>
    public string? CustomConditions { get; set; }

    /// <summary>
    /// Additional message or reason to display when policy is enforced
    /// </summary>
    public string? EnforcementMessage { get; set; }

    /// <summary>
    /// Date and time when this policy becomes active
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Date and time when this policy expires
    /// </summary>
    public DateTime? EffectiveUntil { get; set; }

    /// <summary>
    /// User ID who created this policy
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// User ID who last updated this policy
    /// </summary>
    public Guid? UpdatedBy { get; set; }
}

/// <summary>
/// Types of conditions that can trigger policy enforcement
/// </summary>
public enum PolicyConditionType
{
    /// <summary>
    /// Time-based conditions (time of day, day of week)
    /// </summary>
    Time = 1,

    /// <summary>
    /// Environment-based conditions (production, staging, dev)
    /// </summary>
    Environment = 2,

    /// <summary>
    /// Location-based conditions (country, region, IP range)
    /// </summary>
    Location = 3,

    /// <summary>
    /// Device-based conditions (mobile, desktop, compliance status)
    /// </summary>
    Device = 4,

    /// <summary>
    /// Risk-based conditions (risk score, anomaly detection)
    /// </summary>
    Risk = 5,

    /// <summary>
    /// Composite conditions (multiple condition types combined)
    /// </summary>
    Composite = 6,

    /// <summary>
    /// Custom conditions defined by implementation
    /// </summary>
    Custom = 99
}

/// <summary>
/// Actions to take when a conditional policy matches
/// </summary>
public enum PolicyAction
{
    /// <summary>
    /// Explicitly allow the permission
    /// </summary>
    Allow = 1,

    /// <summary>
    /// Explicitly deny the permission
    /// </summary>
    Deny = 2,

    /// <summary>
    /// Require additional MFA verification
    /// </summary>
    Require2FA = 3,

    /// <summary>
    /// Require approval from designated approver
    /// </summary>
    RequireApproval = 4,

    /// <summary>
    /// Log but allow (audit-only mode)
    /// </summary>
    LogOnly = 5,

    /// <summary>
    /// Challenge with CAPTCHA or similar
    /// </summary>
    Challenge = 6
}
