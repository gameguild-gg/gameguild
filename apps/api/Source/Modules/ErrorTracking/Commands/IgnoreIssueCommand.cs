using GameGuild;
using MediatR;

namespace GameGuild.Modules.ErrorTracking.Commands;

/// <summary>
/// Command to ignore an error issue.
/// </summary>
public record IgnoreIssueCommand(
    Guid IssueId
) : IRequest<Result>;
