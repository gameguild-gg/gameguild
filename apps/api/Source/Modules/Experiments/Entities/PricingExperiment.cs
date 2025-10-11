using GameGuild.Core.Domain;

namespace GameGuild.Modules.Experiments.Entities;

/// <summary>
/// Represents a pricing A/B test experiment
/// </summary>
public class PricingExperiment : EntityBase
{
    // TenantId inherited from EntityBase (no override needed)
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ExperimentStatus Status { get; set; }
    public ExperimentType Type { get; set; }
    public Guid? TargetPlanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int TargetSampleSize { get; set; }
    public double ConfidenceLevel { get; set; } = 0.95;
    public double SignificanceThreshold { get; set; } = 0.05;
    public string? Hypothesis { get; set; }
    public string? Metadata { get; set; }
    public Guid CreatedByUserId { get; set; }

    // Navigation properties
    public ICollection<ExperimentVariant> Variants { get; set; } = new List<ExperimentVariant>();
    public ICollection<UserAssignment> UserAssignments { get; set; } = new List<UserAssignment>();

    // Business methods
    public void Start()
    {
        if (Status != ExperimentStatus.Draft)
            throw new InvalidOperationException("Only draft experiments can be started");

        if (Variants.Count < 2)
            throw new InvalidOperationException("Experiment must have at least 2 variants");

        Status = ExperimentStatus.Active;
        StartDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Pause()
    {
        if (Status != ExperimentStatus.Active)
            throw new InvalidOperationException("Only active experiments can be paused");

        Status = ExperimentStatus.Paused;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resume()
    {
        if (Status != ExperimentStatus.Paused)
            throw new InvalidOperationException("Only paused experiments can be resumed");

        Status = ExperimentStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != ExperimentStatus.Active && Status != ExperimentStatus.Paused)
            throw new InvalidOperationException("Only active or paused experiments can be completed");

        Status = ExperimentStatus.Completed;
        EndDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        if (Status == ExperimentStatus.Active)
            throw new InvalidOperationException("Cannot archive active experiments");

        Status = ExperimentStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsActive() => Status == ExperimentStatus.Active &&
                              (EndDate == null || EndDate > DateTime.UtcNow);

    public bool HasReachedSampleSize()
    {
        var totalAssignments = UserAssignments.Count;
        return totalAssignments >= TargetSampleSize;
    }

    public ExperimentVariant? GetWinningVariant()
    {
        if (Status != ExperimentStatus.Completed)
            return null;

        return Variants
            .OrderByDescending(v => v.ConversionRate)
            .FirstOrDefault();
    }
}

public enum ExperimentStatus
{
    Draft,
    Active,
    Paused,
    Completed,
    Archived
}

public enum ExperimentType
{
    PricingTest,
    FeatureTest,
    BundleTest,
    DiscountTest,
    TrialTest
}
