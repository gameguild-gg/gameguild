using GameGuild.Modules.ErrorTracking.Services;
using GameGuild.CQRS;


namespace GameGuild.Modules.ErrorTracking.Commands;

/// <summary>
/// Handler for resolving error issues.
/// </summary>
public class ResolveIssueCommandHandler : IRequestHandler<ResolveIssueCommand, Result>
{
    private readonly IErrorTrackingService _errorTrackingService;
    private readonly ILogger<ResolveIssueCommandHandler> _logger;

    public ResolveIssueCommandHandler(
        IErrorTrackingService errorTrackingService,
        ILogger<ResolveIssueCommandHandler> logger)
    {
        _errorTrackingService = errorTrackingService;
        _logger = logger;
    }

    public async Task<Result> Handle(ResolveIssueCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _errorTrackingService.ResolveIssueAsync(request.IssueId, request.UserId, request.Notes, cancellationToken);

            _logger.LogInformation("Error issue {IssueId} resolved by user {UserId}", request.IssueId, request.UserId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving issue {IssueId}", request.IssueId);
            return Result.Failure($"Failed to resolve issue: {ex.Message}");
        }
    }
}
