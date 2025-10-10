using GameGuild;
using GameGuild.Modules.ErrorTracking.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.ErrorTracking.Queries;

/// <summary>
/// Query to get events for an error issue.
/// </summary>
public record GetErrorEventsQuery(
    Guid IssueId,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<Result<List<ErrorEventDto>>>;
