using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Commands;

namespace GameGuild.Modules.Permissions.Handlers;

public class RequestJitElevationHandler : IRequestHandler<RequestJitElevationCommand, JitElevationRequest>
{
    private readonly IJitElevationService _jitService;
    private readonly ILogger<RequestJitElevationHandler> _logger;

    public RequestJitElevationHandler(IJitElevationService jitService, ILogger<RequestJitElevationHandler> logger)
    {
        _jitService = jitService;
        _logger = logger;
    }

    public async Task<JitElevationRequest> Handle(RequestJitElevationCommand request, CancellationToken cancellationToken)
    {
        return await _jitService.RequestElevationAsync(
            request.RequesterId,
            request.TenantId,
            request.Permission,
            request.Justification,
            request.DurationMinutes,
            request.ResourceType,
            request.ResourceId,
            request.StartsAt,
            request.RequiresApproval,
            cancellationToken);
    }
}

public class ApproveJitElevationHandler : IRequestHandler<ApproveJitElevationCommand, JitElevationRequest>
{
    private readonly IJitElevationService _jitService;

    public ApproveJitElevationHandler(IJitElevationService jitService)
    {
        _jitService = jitService;
    }

    public async Task<JitElevationRequest> Handle(ApproveJitElevationCommand request, CancellationToken cancellationToken)
    {
        return await _jitService.ApproveElevationAsync(
            request.RequestId,
            request.ReviewerId,
            request.Comments,
            cancellationToken);
    }
}

public class DenyJitElevationHandler : IRequestHandler<DenyJitElevationCommand, JitElevationRequest>
{
    private readonly IJitElevationService _jitService;

    public DenyJitElevationHandler(IJitElevationService jitService)
    {
        _jitService = jitService;
    }

    public async Task<JitElevationRequest> Handle(DenyJitElevationCommand request, CancellationToken cancellationToken)
    {
        return await _jitService.DenyElevationAsync(
            request.RequestId,
            request.ReviewerId,
            request.Comments,
            cancellationToken);
    }
}

public class RevokeJitElevationHandler : IRequestHandler<RevokeJitElevationCommand, bool>
{
    private readonly IJitElevationService _jitService;

    public RevokeJitElevationHandler(IJitElevationService jitService)
    {
        _jitService = jitService;
    }

    public async Task<bool> Handle(RevokeJitElevationCommand request, CancellationToken cancellationToken)
    {
        return await _jitService.RevokeElevationAsync(
            request.RequestId,
            request.ReviewerId,
            request.Reason,
            cancellationToken);
    }
}
