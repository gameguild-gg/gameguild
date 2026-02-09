using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to evaluate a feature flag for a given context
/// </summary>
public sealed record EvaluateFeatureQuery : IQuery<FeatureEvaluationResult>
{
    /// <summary>
    ///     The feature flag key to evaluate
    /// </summary>
    public required string FeatureKey { get; init; }

    /// <summary>
    ///     Optional user ID (defaults to current user)
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    ///     Optional tenant ID (defaults to current tenant)
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    ///     Optional environment (defaults to "production")
    /// </summary>
    public string? Environment { get; init; }

    /// <summary>
    ///     Optional user permissions (defaults to current user's permissions)
    /// </summary>
    public List<string>? Permissions { get; init; }

    /// <summary>
    ///     Optional custom attributes for targeting
    /// </summary>
    public Dictionary<string, object>? CustomAttributes { get; init; }
}
