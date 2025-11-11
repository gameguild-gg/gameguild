using GameGuild.Authentication.Enums;

namespace GameGuild.Authentication.Entities;

/// <summary>
///     Represents a conditional policy that enforces permission rules based on time, environment, or other contextual factors
/// </summary>
public class ConditionalPolicy : EntityBase<Guid>
{
    /// <summary>
    ///     Name of the conditional policy
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Description of what this policy enforces
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Type of condition (Time, Environment, Location, etc.)
    /// </summary>
    public PolicyConditionType ConditionType { get; set; }

    /// <summary>
    ///     Permission type this policy applies to (null means all permissions)
    /// </summary>
    public PermissionType? PermissionType { get; set; }

    /// <summary>
    ///     Target resource type (null means all resource types)
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    ///     Action to take when condition matches (Allow, Deny, Require2FA)
    /// </summary>
    public PolicyAction Action { get; set; }

    /// <summary>
    ///     Priority for policy evaluation (higher = evaluated first)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///     Whether this policy is currently active
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Time-based conditions (JSON serialized)
    ///     Contains: DaysOfWeek, TimeRanges (StartTime, EndTime), TimeZone
    /// </summary>
    public string? TimeConditions { get; set; }

    /// <summary>
    ///     Environment conditions (JSON serialized)
    ///     Contains: Environments (Production, Staging, Development), IpRanges, Countries
    /// </summary>
    public string? EnvironmentConditions { get; set; }

    /// <summary>
    ///     Location conditions (JSON serialized)
    ///     Contains: AllowedCountries, DeniedCountries, AllowedRegions, DeniedRegions
    /// </summary>
    public string? LocationConditions { get; set; }

    /// <summary>
    ///     Device conditions (JSON serialized)
    ///     Contains: AllowedDeviceTypes, RequireCompliancy, RequireEncryption
    /// </summary>
    public string? DeviceConditions { get; set; }

    /// <summary>
    ///     Custom conditions (JSON serialized) for extensibility
    /// </summary>
    public string? CustomConditions { get; set; }

    /// <summary>
    ///     Additional message or reason to display when policy is enforced
    /// </summary>
    public string? EnforcementMessage { get; set; }

    /// <summary>
    ///     Date and time when this policy becomes active
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    ///     Date and time when this policy expires
    /// </summary>
    public DateTime? EffectiveUntil { get; set; }

    /// <summary>
    ///     User ID who created this policy
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    ///     User ID who last updated this policy
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    ///     Check if policy is currently effective (enabled and within date range)
    /// </summary>
    public bool IsEffective()
    {
        if (!IsEnabled) return false;

        var now = DateTime.UtcNow;

        if (EffectiveFrom.HasValue && now < EffectiveFrom.Value) return false;
        if (EffectiveUntil.HasValue && now > EffectiveUntil.Value) return false;

        return true;
    }

    /// <summary>
    ///     Update policy status
    /// </summary>
    public void SetEnabled(bool isEnabled, Guid updatedBy)
    {
        IsEnabled = isEnabled;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Update policy effective period
    /// </summary>
    public void SetEffectivePeriod(DateTime? effectiveFrom, DateTime? effectiveUntil, Guid updatedBy)
    {
        EffectiveFrom = effectiveFrom;
        EffectiveUntil = effectiveUntil;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Update policy conditions
    /// </summary>
    public void UpdateConditions(PolicyConditionType conditionType, string? conditions, Guid updatedBy)
    {
        ConditionType = conditionType;

        switch (conditionType)
        {
            case PolicyConditionType.Time : TimeConditions = conditions; break;
            case PolicyConditionType.Environment : EnvironmentConditions = conditions; break;
            case PolicyConditionType.Location : LocationConditions = conditions; break;
            case PolicyConditionType.Device : DeviceConditions = conditions; break;
            case PolicyConditionType.Custom : CustomConditions = conditions; break;
        }

        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Update policy metadata
    /// </summary>
    public void UpdateMetadata(string name, string? description, int priority, Guid updatedBy)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        Priority = priority;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
