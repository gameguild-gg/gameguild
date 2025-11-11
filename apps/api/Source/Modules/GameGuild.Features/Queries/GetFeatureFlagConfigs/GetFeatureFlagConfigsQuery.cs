using GameGuild.CQRS;
using GameGuild.Features.Models;

namespace GameGuild.Features.Queries;

/// <summary>
///     Query to get feature flag configurations for SDK
/// </summary>
public record GetFeatureFlagConfigsQuery : IQuery<IEnumerable<FeatureFlagConfig>>
{
    public required string Environment { get; init; }

    public string? TenantId { get; init; }

    public IEnumerable<string>? FeatureKeys { get; init; }

    public DateTime? ModifiedSince { get; init; }
}
