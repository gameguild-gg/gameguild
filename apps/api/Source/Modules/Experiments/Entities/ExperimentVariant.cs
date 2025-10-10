using GameGuild.Core.Entities;

namespace GameGuild.Modules.Experiments.Entities;

/// <summary>
/// Represents a variant in a pricing experiment
/// </summary>
public class ExperimentVariant : EntityBase
{
    public Guid ExperimentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsControl { get; set; }
    public int TrafficAllocation { get; set; } = 50; // Percentage
    public decimal? PriceOverride { get; set; }
    public string? PricingConfiguration { get; set; } // JSON
    public string? FeatureFlags { get; set; } // JSON
    public int ImpressionCount { get; set; }
    public int ConversionCount { get; set; }
    public decimal Revenue { get; set; }

    // Navigation properties
    public PricingExperiment Experiment { get; set; } = null!;
    public ICollection<UserAssignment> UserAssignments { get; set; } = new List<UserAssignment>();
    public ICollection<ExperimentResult> Results { get; set; } = new List<ExperimentResult>();

    // Computed properties
    public double ConversionRate => ImpressionCount > 0
        ? (double)ConversionCount / ImpressionCount
        : 0;

    public decimal AverageRevenuePerUser => ConversionCount > 0
        ? Revenue / ConversionCount
        : 0;

    // Business methods
    public void RecordImpression()
    {
        ImpressionCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordConversion(decimal revenue)
    {
        ConversionCount++;
        Revenue += revenue;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatistics(int impressions, int conversions, decimal totalRevenue)
    {
        ImpressionCount = impressions;
        ConversionCount = conversions;
        Revenue = totalRevenue;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsStatisticallySignificant(ExperimentVariant control, double threshold)
    {
        if (IsControl || control.ImpressionCount < 100 || ImpressionCount < 100)
            return false;

        var p1 = control.ConversionRate;
        var p2 = ConversionRate;
        var n1 = control.ImpressionCount;
        var n2 = ImpressionCount;

        // Calculate z-score for two proportions
        var pooledP = (control.ConversionCount + ConversionCount) / (double)(n1 + n2);
        var se = Math.Sqrt(pooledP * (1 - pooledP) * (1.0 / n1 + 1.0 / n2));
        var zScore = Math.Abs((p2 - p1) / se);

        // Z-score > 1.96 means p-value < 0.05 (95% confidence)
        return zScore > 1.96;
    }
}
