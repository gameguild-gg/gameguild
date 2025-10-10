using GameGuild;
using MediatR;

namespace GameGuild.Modules.ErrorTracking.Commands;

/// <summary>
/// Command to delete an error issue.
/// </summary>
public record DeleteIssueCommand(
    Guid IssueId
) : IRequest<Result>;
