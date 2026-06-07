namespace GameGuild.Features;

/// <summary>
///     Data Transfer Object for FeatureFlagUsageSummary
/// </summary>
public record FeatureFlagUsageSummary
{
    public required Guid FeatureFlagId { get; init; }

    public required string FeatureFlagKey { get; init; }

    public required string Name { get; init; }

    public required bool IsEnabled { get; init; }

    public required int TotalEvaluations { get; init; }

    public required int UniqueUsers { get; init; }

    public required DateTime LastEvaluatedAt { get; init; }

    public string? Environment { get; init; }

    public Guid? TenantId { get; init; }

    public required DateTime CreatedAt { get; init; }
}
