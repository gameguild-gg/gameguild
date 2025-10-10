using GameGuild;
using GameGuild.Modules.ErrorTracking.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.ErrorTracking.Commands;

/// <summary>
/// Handler for deleting error issues.
/// </summary>
public class DeleteIssueCommandHandler : IRequestHandler<DeleteIssueCommand, Result>
{
    private readonly IErrorTrackingService _errorTrackingService;
    private readonly ILogger<DeleteIssueCommandHandler> _logger;

    public DeleteIssueCommandHandler(
        IErrorTrackingService errorTrackingService,
        ILogger<DeleteIssueCommandHandler> logger)
    {
        _errorTrackingService = errorTrackingService;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteIssueCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _errorTrackingService.DeleteIssueAsync(request.IssueId, cancellationToken);

            _logger.LogInformation("Error issue {IssueId} deleted", request.IssueId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting issue {IssueId}", request.IssueId);
            return Result.Failure($"Failed to delete issue: {ex.Message}");
        }
    }
}
