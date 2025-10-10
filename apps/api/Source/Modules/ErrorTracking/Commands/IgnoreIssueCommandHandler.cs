using GameGuild;
using GameGuild.Modules.ErrorTracking.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.ErrorTracking.Commands;

/// <summary>
/// Handler for ignoring error issues.
/// </summary>
public class IgnoreIssueCommandHandler : IRequestHandler<IgnoreIssueCommand, Result>
{
    private readonly IErrorTrackingService _errorTrackingService;
    private readonly ILogger<IgnoreIssueCommandHandler> _logger;

    public IgnoreIssueCommandHandler(
        IErrorTrackingService errorTrackingService,
        ILogger<IgnoreIssueCommandHandler> logger)
    {
        _errorTrackingService = errorTrackingService;
        _logger = logger;
    }

    public async Task<Result> Handle(IgnoreIssueCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _errorTrackingService.IgnoreIssueAsync(request.IssueId, cancellationToken);

            _logger.LogInformation("Error issue {IssueId} ignored", request.IssueId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ignoring issue {IssueId}", request.IssueId);
            return Result.Failure($"Failed to ignore issue: {ex.Message}");
        }
    }
}
