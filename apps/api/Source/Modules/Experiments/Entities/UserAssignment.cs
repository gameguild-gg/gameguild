using GameGuild.Core.Domain;

namespace GameGuild.Modules.Experiments.Entities;

/// <summary>
/// Represents a user's assignment to an experiment variant
/// </summary>
public class UserAssignment : EntityBase
{
    public Guid ExperimentId { get; set; }
    public Guid VariantId { get; set; }
    public Guid UserId { get; set; }
    public override Guid? TenantId { get; set; }
    public DateTime AssignedAt { get; set; }
    public bool HasConverted { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public decimal? ConversionRevenue { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? Metadata { get; set; } // JSON

    // Navigation properties
    public PricingExperiment Experiment { get; set; } = null!;
    public ExperimentVariant Variant { get; set; } = null!;

    // Business methods
    public void RecordConversion(decimal revenue)
    {
        if (HasConverted)
            throw new InvalidOperationException("User has already converted");

        HasConverted = true;
        ConvertedAt = DateTime.UtcNow;
        ConversionRevenue = revenue;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsEligibleForReassignment()
    {
        // Users who haven't converted and were assigned more than 24 hours ago
        return !HasConverted &&
               (DateTime.UtcNow - AssignedAt).TotalHours > 24;
    }

    public TimeSpan GetTimeToConversion()
    {
        if (!HasConverted || ConvertedAt == null)
            return TimeSpan.Zero;

        return ConvertedAt.Value - AssignedAt;
    }
}
