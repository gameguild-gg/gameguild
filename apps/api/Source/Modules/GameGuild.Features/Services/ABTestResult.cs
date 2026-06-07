namespace GameGuild.Features;

public class AbTestResult
{
    public string ControlVariant { get; set; } = string.Empty;

    public string TreatmentVariant { get; set; } = string.Empty;

    public double PValue { get; set; }

    public double ZScore { get; set; }

    public bool IsStatisticallySignificant { get; set; }

    public double ConfidenceLevel { get; set; }

    public double RelativeUplift { get; set; }

    public string Recommendation { get; set; } = string.Empty;

    public List<ExperimentStatistics> VariantStats { get; set; } = [];
}
