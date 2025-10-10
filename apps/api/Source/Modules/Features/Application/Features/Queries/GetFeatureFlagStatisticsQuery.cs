using GameGuild.CQRS;
using GameGuild.Modules.Features.DTOs;
namespace GameGuild.Modules.Features.Queries;

/// <summary>
///     Query to get feature flag statistics
/// </summary>
public sealed record GetFeatureFlagStatisticsQuery : IRequest<FeatureFlagStatistics>
{
    public string? Environment { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }
}

