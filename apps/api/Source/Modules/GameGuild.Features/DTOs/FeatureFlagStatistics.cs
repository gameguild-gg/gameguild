namespace GameGuild.Features;

/// <summary>
///     Data Transfer Object for FeatureFlagStatistics
/// </summary>
public record FeatureFlagStatistics
{
    public required Guid FeatureFlagId { get; init; }

    public required string FeatureFlagKey { get; init; }

    public required int TotalEvaluations { get; init; }

    public required int EnabledEvaluations { get; init; }

    public required int DisabledEvaluations { get; init; }

    public required double EnabledPercentage { get; init; }

    public required int UniqueUsers { get; init; }

    public required DateTime FirstEvaluationAt { get; init; }

    public required DateTime LastEvaluationAt { get; init; }

    public required DateTime PeriodStart { get; init; }

    public required DateTime PeriodEnd { get; init; }
}
