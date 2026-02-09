using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to get feature flags by tenant
/// </summary>
public sealed record GetFeatureFlagsByTenantQuery : IQuery<IEnumerable<FeatureFlagDto>>
{
    public required Guid TenantId { get; init; }

    public string? Environment { get; init; }

    public bool? IsEnabled { get; init; }
}
