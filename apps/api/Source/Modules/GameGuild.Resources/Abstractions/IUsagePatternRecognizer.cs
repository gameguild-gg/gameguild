namespace GameGuild.Resources;

/// <summary>
///     Enhances heuristic usage trend analysis with a pluggable pattern recognizer.
/// </summary>
public interface IUsagePatternRecognizer
{
    Task<UsagePatternRecognitionResult> RecognizeAsync(ResourceUsageTrend heuristicTrend, IReadOnlyList<UsageRecord> records, CancellationToken cancellationToken = default);
}
