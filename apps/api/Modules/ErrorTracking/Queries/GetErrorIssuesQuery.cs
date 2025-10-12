using GameGuild.Modules.ErrorTracking.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.ErrorTracking.Queries;

/// <summary>
/// Query to get error issues with filtering.
/// </summary>
public record GetErrorIssuesQuery(
    Guid? TenantId,
    string? Status,
    string? Severity,
    string? Environment,
    DateTime? StartDate,
    DateTime? EndDate,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<Result<List<ErrorIssueDto>>>;
