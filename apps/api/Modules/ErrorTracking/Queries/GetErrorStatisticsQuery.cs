using GameGuild.Modules.ErrorTracking.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.ErrorTracking.Queries;

/// <summary>
/// Query to get error statistics.
/// </summary>
public record GetErrorStatisticsQuery(
    Guid? TenantId,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<Result<ErrorStatisticsDto>>;
