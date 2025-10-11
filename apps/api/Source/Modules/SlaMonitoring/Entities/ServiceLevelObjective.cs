using GameGuild.Core.Domain;

namespace GameGuild.Modules.SlaMonitoring.Entities;

/// <summary>
/// Represents a Service Level Objective (SLO) that defines target performance metrics.
/// </summary>
public class ServiceLevelObjective : EntityBase
{
    /// <summary>
    /// Gets or sets the tenant ID this SLO belongs to.
    /// </summary>
    public override Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the name of the SLO.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what this SLO measures.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the service name this SLO applies to.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target percentage (e.g., 99.9 for 99.9% uptime).
    /// </summary>
    public double TargetPercentage { get; set; }

    /// <summary>
    /// Gets or sets the time window in days for SLO evaluation.
    /// </summary>
    public int TimeWindowDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the error budget percentage (derived from 100 - TargetPercentage).
    /// </summary>
    public double ErrorBudgetPercentage { get; set; }

    /// <summary>
    /// Gets or sets the alert threshold percentage (when to trigger alerts).
    /// </summary>
    public double AlertThresholdPercentage { get; set; } = 50.0;

    /// <summary>
    /// Gets or sets whether this SLO is currently active.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the current status of this SLO.
    /// </summary>
    public SloStatus Status { get; set; } = SloStatus.Active;

    /// <summary>
    /// Gets or sets the last time this SLO was evaluated.
    /// </summary>
    public DateTime? LastEvaluatedAt { get; set; }

    /// <summary>
    /// Gets or sets the current actual percentage based on recent metrics.
    /// </summary>
    public double? CurrentActualPercentage { get; set; }

    /// <summary>
    /// Gets or sets the remaining error budget percentage.
    /// </summary>
    public double? RemainingErrorBudget { get; set; }

    /// <summary>
    /// Gets or sets the collection of service level indicators for this SLO.
    /// </summary>
    public ICollection<ServiceLevelIndicator> Indicators { get; set; } = new List<ServiceLevelIndicator>();

    /// <summary>
    /// Gets or sets the collection of violations for this SLO.
    /// </summary>
    public ICollection<SloViolation> Violations { get; set; } = new List<SloViolation>();

    /// <summary>
    /// Calculates the error budget from the target percentage.
    /// </summary>
    public void CalculateErrorBudget()
    {
        ErrorBudgetPercentage = 100.0 - TargetPercentage;
    }

    /// <summary>
    /// Updates the SLO status based on current metrics.
    /// </summary>
    public void UpdateStatus(double actualPercentage)
    {
        CurrentActualPercentage = actualPercentage;
        RemainingErrorBudget = ErrorBudgetPercentage - (100.0 - actualPercentage);

        if (!IsEnabled)
        {
            Status = SloStatus.Disabled;
        }
        else if (actualPercentage < TargetPercentage)
        {
            Status = SloStatus.Breached;
        }
        else if (RemainingErrorBudget <= (ErrorBudgetPercentage * AlertThresholdPercentage / 100.0))
        {
            Status = SloStatus.AtRisk;
        }
        else
        {
            Status = SloStatus.Active;
        }

        LastEvaluatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the error budget should trigger an alert.
    /// </summary>
    public bool ShouldTriggerAlert()
    {
        if (!IsEnabled || RemainingErrorBudget == null)
            return false;

        var alertBudget = ErrorBudgetPercentage * AlertThresholdPercentage / 100.0;
        return RemainingErrorBudget <= alertBudget;
    }
}
