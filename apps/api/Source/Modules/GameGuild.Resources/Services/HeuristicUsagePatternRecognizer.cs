namespace GameGuild.Resources;

/// <summary>
///     Default recognizer that promotes the service's deterministic heuristic trend classification.
/// </summary>
public sealed class HeuristicUsagePatternRecognizer : IUsagePatternRecognizer
{
    public Task<UsagePatternRecognitionResult> RecognizeAsync(ResourceUsageTrend heuristicTrend, IReadOnlyList<UsageRecord> records, CancellationToken cancellationToken = default)
    {
        var confidence = records.Count switch
        {
            >= 30 => 0.95,
            >= 14 => 0.85,
            >= 7 => 0.75,
            >= 3 => 0.6,
            _ => 0.35
        };

        var metadata = $$"""{"recognizer":"heuristic","sampleSize":{{records.Count}}}""";

        return Task.FromResult(new UsagePatternRecognitionResult(heuristicTrend.Pattern, confidence, metadata));
    }
}
