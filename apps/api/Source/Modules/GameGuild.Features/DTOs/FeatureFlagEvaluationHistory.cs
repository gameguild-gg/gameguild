namespace GameGuild.Features;

/// <summary>
///     Data Transfer Object for FeatureFlagEvaluationHistory
/// </summary>
public record FeatureFlagEvaluationHistory
{
    public required Guid Id { get; init; }

    public required Guid FeatureFlagId { get; init; }

    public required string FeatureFlagKey { get; init; }

    public required string UserId { get; init; }

    public required object EvaluatedValue { get; init; }

    public required bool WasEnabled { get; init; }

    public string? Environment { get; init; }

    public Guid? TenantId { get; init; }

    public string? Context { get; init; }

    public required DateTime EvaluatedAt { get; init; }
}
