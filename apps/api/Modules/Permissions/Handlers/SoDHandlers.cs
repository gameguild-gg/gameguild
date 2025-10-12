using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Commands;
using GameGuild.Modules.Permissions.Entities;

namespace GameGuild.Modules.Permissions.Handlers;

public class CreateSoDRuleHandler : IRequestHandler<CreateSoDRuleCommand, Result<SoDRule>>
{
    private readonly ISoDService _sodService;

    public CreateSoDRuleHandler(ISoDService sodService)
    {
        _sodService = sodService;
    }

    public async Task<Result<SoDRule>> Handle(CreateSoDRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = new SoDRule
        {
            TenantId = request.TenantId,
            Name = request.Name,
            Description = request.Description,
            RuleType = request.RuleType,
            Severity = request.Severity,
            ConflictingPermissions = request.ConflictingPermissions,
            ConflictingRoles = request.ConflictingRoles,
            ConflictingResources = request.ConflictingResources,
            RequireApproval = request.RequireApproval,
            ApproverRoles = request.ApproverRoles,
            CreatedBy = request.CreatedBy
        };
        return await _sodService.CreateRuleAsync(rule, cancellationToken);
    }
}

public class UpdateSoDRuleHandler : IRequestHandler<UpdateSoDRuleCommand, Result<SoDRule>>
{
    private readonly ISoDService _sodService;

    public UpdateSoDRuleHandler(ISoDService sodService)
    {
        _sodService = sodService;
    }

    public async Task<Result<SoDRule>> Handle(UpdateSoDRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = new SoDRule
        {
            Id = request.RuleId,
            Name = request.Name,
            Description = request.Description,
            RuleType = request.RuleType,
            Severity = request.Severity,
            IsEnabled = request.IsEnabled,
            ConflictingPermissions = request.ConflictingPermissions,
            ConflictingRoles = request.ConflictingRoles,
            ConflictingResources = request.ConflictingResources,
            RequireApproval = request.RequireApproval,
            ApproverRoles = request.ApproverRoles
        };
        return await _sodService.UpdateRuleAsync(rule, cancellationToken);
    }
}

public class DeleteSoDRuleHandler : IRequestHandler<DeleteSoDRuleCommand, Result>
{
    private readonly ISoDService _sodService;

    public DeleteSoDRuleHandler(ISoDService sodService)
    {
        _sodService = sodService;
    }

    public async Task<Result> Handle(DeleteSoDRuleCommand request, CancellationToken cancellationToken)
    {
        return await _sodService.DeleteRuleAsync(request.RuleId, cancellationToken);
    }
}

public class GetSoDRuleHandler : IRequestHandler<GetSoDRuleQuery, Result<SoDRule>>
{
    private readonly ISoDService _sodService;

    public GetSoDRuleHandler(ISoDService sodService)
    {
        _sodService = sodService;
    }

    public async Task<Result<SoDRule>> Handle(GetSoDRuleQuery request, CancellationToken cancellationToken)
    {
        return await _sodService.GetRuleAsync(request.RuleId, cancellationToken);
    }
}

public class ListSoDRulesHandler : IRequestHandler<ListSoDRulesQuery, Result<List<SoDRule>>>
{
    private readonly ISoDService _sodService;

    public ListSoDRulesHandler(ISoDService sodService)
    {
        _sodService = sodService;
    }

    public async Task<Result<List<SoDRule>>> Handle(ListSoDRulesQuery request, CancellationToken cancellationToken)
    {
        return await _sodService.ListRulesAsync(request.TenantId, cancellationToken);
    }
}

public class DetectSoDViolationsHandler : IRequestHandler<DetectSoDViolationsCommand, Result<List<SoDViolation>>>
{
    private readonly ISoDService _sodService;

    public DetectSoDViolationsHandler(ISoDService sodService)
    {
        _sodService = sodService;
    }

    public async Task<Result<List<SoDViolation>>> Handle(DetectSoDViolationsCommand request, CancellationToken cancellationToken)
    {
        return await _sodService.DetectViolationsAsync(request.UserId, request.TenantId, cancellationToken);
    }
}

public class ResolveSoDViolationHandler : IRequestHandler<ResolveSoDViolationCommand, Result<SoDViolation>>
{
    private readonly ISoDService _sodService;

    public ResolveSoDViolationHandler(ISoDService sodService)
    {
        _sodService = sodService;
    }

    public async Task<Result<SoDViolation>> Handle(ResolveSoDViolationCommand request, CancellationToken cancellationToken)
    {
        return await _sodService.ResolveViolationAsync(request.ViolationId, request.Action, request.Notes, request.ResolvedBy, cancellationToken);
    }
}

public class GetActiveSoDViolationsHandler : IRequestHandler<GetActiveSoDViolationsQuery, Result<List<SoDViolation>>>
{
    private readonly ISoDService _sodService;

    public GetActiveSoDViolationsHandler(ISoDService sodService)
    {
        _sodService = sodService;
    }

    public async Task<Result<List<SoDViolation>>> Handle(GetActiveSoDViolationsQuery request, CancellationToken cancellationToken)
    {
        return await _sodService.GetActiveViolationsAsync(request.TenantId, cancellationToken);
    }
}

public class ScanAllUsersForSoDHandler : IRequestHandler<ScanAllUsersForSoDCommand, Result>
{
    private readonly ISoDService _sodService;

    public ScanAllUsersForSoDHandler(ISoDService sodService)
    {
        _sodService = sodService;
    }

    public async Task<Result> Handle(ScanAllUsersForSoDCommand request, CancellationToken cancellationToken)
    {
        return await _sodService.ScanAllUsersAsync(request.TenantId, cancellationToken);
    }
}

public class GetSoDStatisticsHandler : IRequestHandler<GetSoDStatisticsQuery, Result<SoDStatistics>>
{
    private readonly ISoDService _sodService;

    public GetSoDStatisticsHandler(ISoDService sodService)
    {
        _sodService = sodService;
    }

    public async Task<Result<SoDStatistics>> Handle(GetSoDStatisticsQuery request, CancellationToken cancellationToken)
    {
        return await _sodService.GetStatisticsAsync(request.TenantId, cancellationToken);
    }
}
