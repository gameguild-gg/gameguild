namespace GameGuild.Features;

/// <summary>
///     Service for A/B test statistical evaluation and significance testing
/// </summary>
public interface IFeatureFlagExperimentService
{
    /// <summary>
    ///     Evaluates A/B test results and determines statistical significance
    /// </summary>
    AbTestResult EvaluateAbTest(Dictionary<string, (int observations, int conversions)> variants, double confidenceLevel = 0.95);

    /// <summary>
    ///     Calculates required sample size for desired statistical power
    /// </summary>
    int CalculateRequiredSampleSize(double baselineConversionRate, double minimumDetectableEffect, double power = 0.8, double alpha = 0.05);

    /// <summary>
    ///     Determines if an experiment has reached statistical significance
    /// </summary>
    bool HasReachedSignificance(int controlConversions, int controlObservations, int treatmentConversions, int treatmentObservations, double alpha = 0.05);
}
