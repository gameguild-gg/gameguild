using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Collections.Concurrent;

namespace GameGuild.API;

internal static class RateLimitingMetrics
{
    internal const string MeterName = "GameGuild.API.RateLimiting";
    internal const string RejectionsInstrumentName = "gameguild.api.rate_limit.rejections";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Rejections = Meter.CreateCounter<long>(
        RejectionsInstrumentName,
        unit: "requests",
        description: "Number of requests rejected by API rate limits.");
    private static readonly ConcurrentDictionary<(string Policy, string Enforcement), long> RejectionCounts = new();

    internal static void RecordRejection(string policy, string enforcement)
    {
        var tags = new TagList
        {
            { "policy", policy },
            { "enforcement", enforcement }
        };

        Rejections.Add(1, tags);
        RejectionCounts.AddOrUpdate((policy, enforcement), 1, static (_, count) => count + 1);
    }

    internal static IReadOnlyList<(string Policy, string Enforcement, long Count)> GetRejectionSnapshot()
        => RejectionCounts
            .Select(entry => (entry.Key.Policy, entry.Key.Enforcement, entry.Value))
            .OrderBy(entry => entry.Policy, StringComparer.Ordinal)
            .ThenBy(entry => entry.Enforcement, StringComparer.Ordinal)
            .ToArray();
}
