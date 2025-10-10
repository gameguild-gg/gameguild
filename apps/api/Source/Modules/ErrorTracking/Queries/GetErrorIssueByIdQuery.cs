using GameGuild;
using GameGuild.Modules.ErrorTracking.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.ErrorTracking.Queries;

/// <summary>
/// Query to get a single error issue by ID.
/// </summary>
public record GetErrorIssueByIdQuery(
    Guid IssueId
) : IRequest<Result<ErrorIssueDto>>;
