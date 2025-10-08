using MediatR;
using GameGuild.Modules.Features.DTOs;
namespace GameGuild.Modules.Features.Queries;

/// <summary>
///     Query to get feature flag evaluation history
/// </summary>
public sealed record GetFeatureFlagEvaluationHistoryQuery : IRequest<PagedResult<FeatureFlagEvaluationHistory>>
{
    public required string FeatureKey { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    public Guid? TenantId { get; init; }

    public Guid? UserId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}

