using GameGuild;
using GameGuild.Modules.ErrorTracking.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.ErrorTracking.Queries;

/// <summary>
/// Handler for getting events for an error issue.
/// </summary>
public class GetErrorEventsQueryHandler : IRequestHandler<GetErrorEventsQuery, Result<List<ErrorEventDto>>>
{
    private readonly IErrorTrackingService _errorTrackingService;
    private readonly ILogger<GetErrorEventsQueryHandler> _logger;

    public GetErrorEventsQueryHandler(
        IErrorTrackingService errorTrackingService,
        ILogger<GetErrorEventsQueryHandler> logger)
    {
        _errorTrackingService = errorTrackingService;
        _logger = logger;
    }

    public async Task<Result<List<ErrorEventDto>>> Handle(GetErrorEventsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var events = await _errorTrackingService.GetIssueEventsAsync(
                request.IssueId,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            return Result<List<ErrorEventDto>>.Success(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events for issue {IssueId}", request.IssueId);
            return Result<List<ErrorEventDto>>.Failure($"Failed to retrieve events: {ex.Message}");
        }
    }
}
