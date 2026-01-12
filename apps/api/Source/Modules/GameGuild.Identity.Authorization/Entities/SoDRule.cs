using GameGuild.CQRS.Models;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Represents a Separation of Duties (SoD) rule
/// </summary>
public class SoDRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public TenantId? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public SoDRuleType RuleType { get; set; }

    public SoDSeverity Severity { get; set; }

    public bool IsEnabled { get; set; } = true;

    // Stored as JSON arrays
    public string ConflictingPermissions { get; set; } = string.Empty;

    public string? ConflictingRoles { get; set; }

    public string? ConflictingResources { get; set; }

    public string? AllowedExceptions { get; set; }

    public bool RequireApproval { get; set; }

    public string? ApproverRoles { get; set; }

    public string? MitigationStrategy { get; set; }

    public int ViolationCount { get; set; }

    public DateTime? LastViolationDetected { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<SoDViolation> Violations { get; set; } = new List<SoDViolation>();

    /// <summary>
    ///     Check if rule is active
    /// </summary>
    public bool IsActive() => IsEnabled;

    /// <summary>
    ///     Enable the rule
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Disable the rule
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Record a violation detected
    /// </summary>
    public void RecordViolation()
    {
        ViolationCount++;
        LastViolationDetected = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Check if rule is critical
    /// </summary>
    public bool IsCritical() => Severity == SoDSeverity.Critical;

    /// <summary>
    ///     Check if rule is high severity
    /// </summary>
    public bool IsHighSeverity() => Severity is SoDSeverity.Critical or SoDSeverity.High;
}

/// <summary>
///     Represents a detected SoD violation
/// </summary>
public class SoDViolation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RuleId { get; set; }

    public Guid UserId { get; set; }

    public TenantId? TenantId { get; set; }

    public SoDViolationStatus Status { get; set; } = SoDViolationStatus.Active;

    public string ViolationDetails { get; set; } = string.Empty;

    public string ConflictingItems { get; set; } = string.Empty;

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public Guid? DetectedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public Guid? ResolvedBy { get; set; }

    public string? ResolutionNotes { get; set; }

    public SoDResolutionAction? ResolutionAction { get; set; }

    public bool IsException { get; set; }

    public string? ExceptionJustification { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public SoDRule Rule { get; set; } = null!;

    /// <summary>
    ///     Check if violation is active
    /// </summary>
    public bool IsActive() => Status == SoDViolationStatus.Active;

    /// <summary>
    ///     Check if violation is resolved
    /// </summary>
    public bool IsResolved() => Status == SoDViolationStatus.Resolved;

    /// <summary>
    ///     Resolve the violation
    /// </summary>
    public void Resolve(Guid resolvedBy, SoDResolutionAction action, string? notes = null)
    {
        Status = SoDViolationStatus.Resolved;
        ResolvedBy = resolvedBy;
        ResolvedAt = DateTime.UtcNow;
        ResolutionAction = action;
        ResolutionNotes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Mark as exception (approved violation)
    /// </summary>
    public void MarkAsException(Guid approvedBy, string justification)
    {
        Status = SoDViolationStatus.Excepted;
        IsException = true;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        ExceptionJustification = justification;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Acknowledge the violation
    /// </summary>
    public void Acknowledge()
    {
        Status = SoDViolationStatus.Acknowledged;
        UpdatedAt = DateTime.UtcNow;
    }
}
