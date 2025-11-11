using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Resources.Entities;

/// <summary>
///     SLA impact analysis for tracking violations and performance metrics
/// </summary>
[Table("SlaImpactAnalyses")]
public class SlaImpactAnalysis : EntityBase
{
    /// <summary>
    ///     Resource quota that was affected
    /// </summary>
    [Required]
    public Guid ResourceQuotaId { get; set; }

    /// <summary>
    ///     Navigation property to ResourceQuota
    /// </summary>
    [ForeignKey(nameof(ResourceQuotaId))]
    public ResourceQuota? ResourceQuota { get; set; }

    /// <summary>
    ///     User affected by the SLA violation (if applicable)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    ///     When the violation started
    /// </summary>
    [Required]
    public DateTime ViolationStartTime { get; set; }

    /// <summary>
    ///     When the violation ended (null if ongoing)
    /// </summary>
    public DateTime? ViolationEndTime { get; set; }

    /// <summary>
    ///     Duration of the violation in seconds
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    ///     Severity of the SLA violation
    /// </summary>
    [Required]
    public SlaViolationSeverity Severity { get; set; } = SlaViolationSeverity.Medium;

    /// <summary>
    ///     Type of SLA violation
    /// </summary>
    [Required]
    public SlaViolationType ViolationType { get; set; }

    /// <summary>
    ///     Expected SLA target value
    /// </summary>
    public long ExpectedValue { get; set; }

    /// <summary>
    ///     Actual value that violated SLA
    /// </summary>
    public long ActualValue { get; set; }

    /// <summary>
    ///     Deviation percentage from SLA target
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DeviationPercentage { get; set; }

    /// <summary>
    ///     Estimated business impact
    /// </summary>
    [MaxLength(500)]
    public string? BusinessImpact { get; set; }

    /// <summary>
    ///     Root cause analysis
    /// </summary>
    [MaxLength(1000)]
    public string? RootCause { get; set; }

    /// <summary>
    ///     Mitigation actions taken
    /// </summary>
    [MaxLength(1000)]
    public string? MitigationActions { get; set; }

    /// <summary>
    ///     Whether the violation has been resolved
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    ///     When the violation was resolved
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    ///     Who resolved the violation
    /// </summary>
    public Guid? ResolvedByUserId { get; set; }

    /// <summary>
    ///     Whether this requires escalation
    /// </summary>
    public bool RequiresEscalation { get; set; }

    /// <summary>
    ///     Whether an incident ticket was created
    /// </summary>
    public bool IncidentCreated { get; set; }

    /// <summary>
    ///     External incident ticket reference
    /// </summary>
    [MaxLength(100)]
    public string? IncidentTicketId { get; set; }

    /// <summary>
    ///     Additional metadata as JSON
    /// </summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }

    // Note: TenantId is inherited from EntityBase

    /// <summary>
    ///     Calculate duration if violation has ended
    /// </summary>
    public void CalculateDuration()
    {
        if (ViolationEndTime.HasValue) { DurationSeconds = (int) (ViolationEndTime.Value - ViolationStartTime).TotalSeconds; }
    }

    /// <summary>
    ///     Calculate deviation percentage based on expected and actual values
    /// </summary>
    public void CalculateDeviation()
    {
        if (ExpectedValue > 0) { DeviationPercentage = Math.Round((ActualValue - ExpectedValue) / (decimal) ExpectedValue * 100, 2); }
    }

    /// <summary>
    ///     Mark violation as resolved
    /// </summary>
    public void Resolve(Guid resolvedByUserId, string? mitigationActions = null)
    {
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
        ResolvedByUserId = resolvedByUserId;
        ViolationEndTime ??= DateTime.UtcNow;

        if (!string.IsNullOrEmpty(mitigationActions)) { MitigationActions = mitigationActions; }

        CalculateDuration();
    }

    /// <summary>
    ///     Determine if this violation is critical and ongoing
    /// </summary>
    public bool IsCriticalAndOngoing() { return Severity == SlaViolationSeverity.Critical && !IsResolved && !ViolationEndTime.HasValue; }

    /// <summary>
    ///     Check if violation exceeds a specified duration threshold
    /// </summary>
    public bool ExceedsDuration(int thresholdMinutes)
    {
        var endTime = ViolationEndTime ?? DateTime.UtcNow;
        var duration = (endTime - ViolationStartTime).TotalMinutes;

        return duration > thresholdMinutes;
    }
}
