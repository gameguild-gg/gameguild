using GameGuild.CQRS;
using GameGuild.Modules.Features.DTOs;
namespace GameGuild.Modules.Features.Queries;

/// <summary>
///     Query to get feature flags by tenant
/// </summary>
public sealed record GetFeatureFlagsByTenantQuery : IRequest<IEnumerable<FeatureFlagDto>>
{
    public required Guid TenantId { get; init; }

    public string? Environment { get; init; }

    public bool? IsEnabled { get; init; }
}

