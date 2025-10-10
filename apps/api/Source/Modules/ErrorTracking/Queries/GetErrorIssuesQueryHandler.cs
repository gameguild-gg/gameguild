using GameGuild;
using GameGuild.Modules.ErrorTracking.Services;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.ErrorTracking.Queries;

/// <summary>
/// Handler for getting error issues with filtering.
/// </summary>
public class GetErrorIssuesQueryHandler : IRequestHandler<GetErrorIssuesQuery, Result<List<ErrorIssueDto>>>
{
    private readonly IErrorTrackingService _errorTrackingService;
    private readonly ILogger<GetErrorIssuesQueryHandler> _logger;

    public GetErrorIssuesQueryHandler(
        IErrorTrackingService errorTrackingService,
        ILogger<GetErrorIssuesQueryHandler> logger)
    {
        _errorTrackingService = errorTrackingService;
        _logger = logger;
    }

    public async Task<Result<List<ErrorIssueDto>>> Handle(GetErrorIssuesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var getRequest = new GetIssuesRequest(
                request.TenantId,
                request.Status,
                request.Severity,
                request.Environment,
                request.StartDate,
                request.EndDate,
                request.PageNumber,
                request.PageSize
            );

            var issues = await _errorTrackingService.GetIssuesAsync(getRequest, cancellationToken);

            return Result<List<ErrorIssueDto>>.Success(issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving error issues");
            return Result<List<ErrorIssueDto>>.Failure($"Failed to retrieve issues: {ex.Message}");
        }
    }
}
