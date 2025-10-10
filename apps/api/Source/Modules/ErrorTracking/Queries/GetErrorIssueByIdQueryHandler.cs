using GameGuild;
using GameGuild.Modules.ErrorTracking.Services;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.ErrorTracking.Queries;

/// <summary>
/// Handler for getting a single error issue by ID.
/// </summary>
public class GetErrorIssueByIdQueryHandler : IRequestHandler<GetErrorIssueByIdQuery, Result<ErrorIssueDto>>
{
    private readonly IErrorTrackingService _errorTrackingService;
    private readonly ILogger<GetErrorIssueByIdQueryHandler> _logger;

    public GetErrorIssueByIdQueryHandler(
        IErrorTrackingService errorTrackingService,
        ILogger<GetErrorIssueByIdQueryHandler> logger)
    {
        _errorTrackingService = errorTrackingService;
        _logger = logger;
    }

    public async Task<Result<ErrorIssueDto>> Handle(GetErrorIssueByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var issue = await _errorTrackingService.GetIssueAsync(request.IssueId, cancellationToken);

            if (issue == null)
            {
                return Result<ErrorIssueDto>.Failure($"Issue {request.IssueId} not found");
            }

            return Result<ErrorIssueDto>.Success(issue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving issue {IssueId}", request.IssueId);
            return Result<ErrorIssueDto>.Failure($"Failed to retrieve issue: {ex.Message}");
        }
    }
}
