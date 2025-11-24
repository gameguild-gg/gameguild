using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Domain.Entities;

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
    public bool IsActive() { return IsEnabled; }

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
    public bool IsCritical() { return Severity == SoDSeverity.Critical; }

    /// <summary>
    ///     Check if rule is high severity
    /// </summary>
    public bool IsHighSeverity() { return Severity is SoDSeverity.Critical or SoDSeverity.High; }
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
    public bool IsActive() { return Status == SoDViolationStatus.Active; }

    /// <summary>
    ///     Check if violation is resolved
    /// </summary>
    public bool IsResolved() { return Status == SoDViolationStatus.Resolved; }

    /// <summary>
    ///     Acknowledge the violation
    /// </summary>
    public void Acknowledge()
    {
        if (Status != SoDViolationStatus.Active) throw new InvalidOperationException("Only active violations can be acknowledged");

        Status = SoDViolationStatus.Acknowledged;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Resolve the violation
    /// </summary>
    public void Resolve(Guid resolvedBy, SoDResolutionAction action, string notes)
    {
        if (Status == SoDViolationStatus.Resolved) throw new InvalidOperationException("Violation is already resolved");

        Status = SoDViolationStatus.Resolved;
        ResolvedBy = resolvedBy;
        ResolvedAt = DateTime.UtcNow;
        ResolutionAction = action;
        ResolutionNotes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Grant exception for the violation
    /// </summary>
    public void GrantException(Guid approvedBy, string justification)
    {
        Status = SoDViolationStatus.Excepted;
        IsException = true;
        ExceptionJustification = justification;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Mark as false positive
    /// </summary>
    public void MarkAsFalsePositive(Guid resolvedBy, string reason)
    {
        Status = SoDViolationStatus.FalsePositive;
        ResolvedBy = resolvedBy;
        ResolvedAt = DateTime.UtcNow;
        ResolutionNotes = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Mitigate the violation
    /// </summary>
    public void Mitigate(string mitigationDetails)
    {
        Status = SoDViolationStatus.Mitigated;
        ResolutionNotes = mitigationDetails;
        UpdatedAt = DateTime.UtcNow;
    }
}
