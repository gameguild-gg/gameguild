using GameGuild.Authentication.Enums;

namespace GameGuild.Authentication.Entities;

/// <summary>
///     ABAC (Attribute-Based Access Control) Policy entity
///     Defines rules for fine-grained access control based on attributes
/// </summary>
public class AbacPolicy : EntityBase<Guid>
{
    /// <summary>
    ///     Name of the ABAC policy
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Description of what this policy controls
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Target resource type (null means all resource types)
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    ///     Permission type this policy applies to (null means all permissions)
    /// </summary>
    public PermissionType? PermissionType { get; set; }

    /// <summary>
    ///     Policy effect (Allow or Deny)
    /// </summary>
    public AbacPolicyEffect Effect { get; set; } = AbacPolicyEffect.Allow;

    /// <summary>
    ///     Priority for policy evaluation (higher = evaluated first)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///     Whether this policy is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Attribute expression in JSON format that defines the conditions
    ///     Example: {"user.department": "IT", "resource.confidentiality": "public"}
    /// </summary>
    public string AttributeExpression { get; set; } = string.Empty;

    /// <summary>
    ///     Optional condition expression for complex logic
    ///     Example: "user.clearanceLevel >= resource.requiredClearance"
    /// </summary>
    public string? ConditionExpression { get; set; }

    /// <summary>
    ///     Target environments where this policy applies
    ///     JSON array: ["Production", "Staging"]
    /// </summary>
    public string? TargetEnvironments { get; set; }

    /// <summary>
    ///     Time-based conditions (JSON serialized)
    ///     Contains: DaysOfWeek, TimeRanges, TimeZone
    /// </summary>
    public string? TimeConditions { get; set; }

    /// <summary>
    ///     Location-based conditions (JSON serialized)
    ///     Contains: AllowedCountries, AllowedRegions, IpRanges
    /// </summary>
    public string? LocationConditions { get; set; }

    /// <summary>
    ///     Obligations that must be fulfilled if policy matches
    ///     JSON array of obligation strings
    /// </summary>
    public string? Obligations { get; set; }

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
    ///     Version number for policy updates
    /// </summary>
    public new int Version { get; set; } = 1;

    /// <summary>
    ///     Tags for policy categorization and searching
    ///     JSON array of tag strings
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    ///     Check if policy is currently effective (active and within date range)
    /// </summary>
    public bool IsEffective()
    {
        if (!IsActive) return false;

        var now = DateTime.UtcNow;

        if (EffectiveFrom.HasValue && now < EffectiveFrom.Value) return false;
        if (EffectiveUntil.HasValue && now > EffectiveUntil.Value) return false;

        return true;
    }

    /// <summary>
    ///     Update policy status
    /// </summary>
    public void SetActive(bool isActive, Guid updatedBy)
    {
        IsActive = isActive;
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
    ///     Update policy expression
    /// </summary>
    public void UpdateExpression(string attributeExpression, string? conditionExpression, Guid updatedBy)
    {
        AttributeExpression = attributeExpression ?? throw new ArgumentNullException(nameof(attributeExpression));
        ConditionExpression = conditionExpression;
        UpdatedBy = updatedBy;
        Version++;
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
