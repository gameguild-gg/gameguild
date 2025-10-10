using GameGuild;
using GameGuild.Modules.ErrorTracking.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.ErrorTracking.Commands;

/// <summary>
/// Handler for capturing error events.
/// </summary>
public class CaptureErrorCommandHandler : IRequestHandler<CaptureErrorCommand, Result<Guid>>
{
    private readonly IErrorTrackingService _errorTrackingService;
    private readonly ILogger<CaptureErrorCommandHandler> _logger;

    public CaptureErrorCommandHandler(
        IErrorTrackingService errorTrackingService,
        ILogger<CaptureErrorCommandHandler> logger)
    {
        _errorTrackingService = errorTrackingService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CaptureErrorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var captureRequest = new CaptureErrorRequest(
                request.TenantId,
                request.ExceptionType,
                request.Message,
                request.StackTrace,
                request.Severity,
                request.Environment,
                request.Release,
                request.UserId,
                request.Url,
                request.HttpMethod,
                request.UserAgent,
                request.IpAddress,
                request.Tags,
                request.ContextData,
                request.Breadcrumbs
            );

            var eventId = await _errorTrackingService.CaptureErrorAsync(captureRequest, cancellationToken);

            _logger.LogInformation("Error event {EventId} captured successfully", eventId);

            return Result<Guid>.Success(eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing error event");
            return Result<Guid>.Failure($"Failed to capture error: {ex.Message}");
        }
    }
}
