namespace GameGuild.Features;

/// <summary>
///     Data Transfer Object for FeatureFlagDependency
/// </summary>
public record FeatureFlagDependency
{
    public required Guid Id { get; init; }

    public required Guid FeatureFlagId { get; init; }

    public required Guid DependsOnFeatureFlagId { get; init; }

    public required string DependencyType { get; init; }

    public required string FeatureFlagKey { get; init; }

    public required string DependsOnFeatureFlagKey { get; init; }

    public required DateTime CreatedAt { get; init; }
}
