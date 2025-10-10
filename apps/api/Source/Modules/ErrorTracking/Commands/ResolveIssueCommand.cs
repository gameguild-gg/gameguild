using GameGuild;
using MediatR;

namespace GameGuild.Modules.ErrorTracking.Commands;

/// <summary>
/// Command to resolve an error issue.
/// </summary>
public record ResolveIssueCommand(
    Guid IssueId,
    Guid UserId,
    string? Notes
) : IRequest<Result>;
