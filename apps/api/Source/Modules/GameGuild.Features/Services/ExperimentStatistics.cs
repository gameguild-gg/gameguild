namespace GameGuild.Features;

/// <summary>
///     Statistical models for A/B test evaluation
/// </summary>
public class ExperimentStatistics
{
    public string VariantName { get; set; } = string.Empty;

    public int Observations { get; set; }

    public int Conversions { get; set; }

    public double ConversionRate { get; set; }

    public double StandardError { get; set; }

    public double ConfidenceIntervalLower { get; set; }

    public double ConfidenceIntervalUpper { get; set; }

    public bool IsControl { get; set; }
}
