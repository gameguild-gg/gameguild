using GameGuild.Core.Entities;

namespace GameGuild.Modules.Experiments.Entities;

/// <summary>
/// Represents statistical results for an experiment variant
/// </summary>
public class ExperimentResult : EntityBase
{
    public Guid ExperimentId { get; set; }
    public Guid VariantId { get; set; }
    public DateTime CalculatedAt { get; set; }
    public int SampleSize { get; set; }
    public double ConversionRate { get; set; }
    public double ConfidenceLevel { get; set; }
    public double PValue { get; set; }
    public double ZScore { get; set; }
    public bool IsStatisticallySignificant { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageRevenuePerUser { get; set; }
    public double StandardError { get; set; }
    public double LowerBound { get; set; }
    public double UpperBound { get; set; }
    public double Lift { get; set; } // Percentage improvement over control
    public string? Notes { get; set; }

    // Navigation properties
    public PricingExperiment Experiment { get; set; } = null!;
    public ExperimentVariant Variant { get; set; } = null!;

    // Business methods
    public static ExperimentResult Calculate(
        ExperimentVariant variant,
        ExperimentVariant? control,
        double confidenceLevel)
    {
        var result = new ExperimentResult
        {
            Id = Guid.NewGuid(),
            ExperimentId = variant.ExperimentId,
            VariantId = variant.Id,
            CalculatedAt = DateTime.UtcNow,
            SampleSize = variant.ImpressionCount,
            ConversionRate = variant.ConversionRate,
            ConfidenceLevel = confidenceLevel,
            TotalRevenue = variant.Revenue,
            AverageRevenuePerUser = variant.AverageRevenuePerUser,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (control != null && !variant.IsControl)
        {
            // Calculate statistical significance
            var p1 = control.ConversionRate;
            var p2 = variant.ConversionRate;
            var n1 = control.ImpressionCount;
            var n2 = variant.ImpressionCount;

            if (n1 >= 100 && n2 >= 100)
            {
                var pooledP = (control.ConversionCount + variant.ConversionCount) / (double)(n1 + n2);
                result.StandardError = Math.Sqrt(pooledP * (1 - pooledP) * (1.0 / n1 + 1.0 / n2));
                result.ZScore = Math.Abs((p2 - p1) / result.StandardError);

                // Calculate p-value from z-score (two-tailed test)
                result.PValue = 2 * (1 - NormalCDF(result.ZScore));
                result.IsStatisticallySignificant = result.PValue < (1 - confidenceLevel);

                // Calculate confidence interval
                var margin = 1.96 * result.StandardError; // 95% confidence
                result.LowerBound = p2 - margin;
                result.UpperBound = p2 + margin;

                // Calculate lift
                result.Lift = p1 > 0 ? ((p2 - p1) / p1) * 100 : 0;
            }
        }

        return result;
    }

    // Normal cumulative distribution function (approximation)
    private static double NormalCDF(double x)
    {
        return 0.5 * (1 + Erf(x / Math.Sqrt(2)));
    }

    // Error function approximation
    private static double Erf(double x)
    {
        var sign = x < 0 ? -1 : 1;
        x = Math.Abs(x);

        var a1 = 0.254829592;
        var a2 = -0.284496736;
        var a3 = 1.421413741;
        var a4 = -1.453152027;
        var a5 = 1.061405429;
        var p = 0.3275911;

        var t = 1.0 / (1.0 + p * x);
        var y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

        return sign * y;
    }

    public string GetResultSummary()
    {
        if (IsStatisticallySignificant)
        {
            return $"Statistically significant with {Lift:F2}% lift (p={PValue:F4})";
        }
        return $"Not statistically significant (p={PValue:F4})";
    }
}
