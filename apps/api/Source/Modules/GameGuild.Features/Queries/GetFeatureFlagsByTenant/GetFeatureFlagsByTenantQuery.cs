using GameGuild.CQRS;
using GameGuild.Features.DTOs;

namespace GameGuild.Features.Queries;

/// <summary>
///     Query to get feature flags by tenant
/// </summary>
public record GetFeatureFlagsByTenantQuery : IQuery<IEnumerable<FeatureFlagDto>>
{
    public required Guid TenantId { get; init; }

    public string? Environment { get; init; }

    public bool? IsEnabled { get; init; }
}
