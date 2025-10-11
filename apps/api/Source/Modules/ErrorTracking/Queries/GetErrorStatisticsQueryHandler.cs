using GameGuild.Modules.ErrorTracking.Services;
using GameGuild.CQRS;


namespace GameGuild.Modules.ErrorTracking.Queries;

/// <summary>
/// Handler for getting error statistics.
/// </summary>
public class GetErrorStatisticsQueryHandler : IRequestHandler<GetErrorStatisticsQuery, Result<ErrorStatisticsDto>>
{
    private readonly IErrorTrackingService _errorTrackingService;
    private readonly ILogger<GetErrorStatisticsQueryHandler> _logger;

    public GetErrorStatisticsQueryHandler(
        IErrorTrackingService errorTrackingService,
        ILogger<GetErrorStatisticsQueryHandler> logger)
    {
        _errorTrackingService = errorTrackingService;
        _logger = logger;
    }

    public async Task<Result<ErrorStatisticsDto>> Handle(GetErrorStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var statistics = await _errorTrackingService.GetStatisticsAsync(
                request.TenantId,
                request.StartDate,
                request.EndDate,
                cancellationToken);

            return Result<ErrorStatisticsDto>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving error statistics");
            return Result<ErrorStatisticsDto>.Failure($"Failed to retrieve statistics: {ex.Message}");
        }
    }
}
