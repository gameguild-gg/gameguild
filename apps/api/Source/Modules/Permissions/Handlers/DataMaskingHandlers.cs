using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Commands;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Entities;
using GameGuild.Shared;

namespace GameGuild.Modules.Permissions.Handlers;

public class CreateDataMaskingRuleHandler : IRequestHandler<CreateDataMaskingRuleCommand, Result<DataMaskingRule>>
{
    private readonly IDataMaskingService _maskingService;

    public CreateDataMaskingRuleHandler(IDataMaskingService maskingService)
    {
        _maskingService = maskingService;
    }

    public async Task<Result<DataMaskingRule>> Handle(
        CreateDataMaskingRuleCommand request,
        CancellationToken cancellationToken)
    {
        var rule = new DataMaskingRule
        {
            Name = request.Name,
            Description = request.Description,
            TenantId = request.TenantId,
            ResourceType = request.ResourceType,
            FieldName = request.FieldName,
            MaskingType = request.MaskingType,
            MaskingPattern = request.MaskingPattern,
            ExemptUserIdsJson = request.ExemptUserIds != null ? System.Text.Json.JsonSerializer.Serialize(request.ExemptUserIds) : null,
            ExemptRoleIdsJson = request.ExemptRoleIds != null ? System.Text.Json.JsonSerializer.Serialize(request.ExemptRoleIds) : null,
            ExemptPermissionsJson = request.ExemptPermissions != null ? System.Text.Json.JsonSerializer.Serialize(request.ExemptPermissions) : null,
            IsEnabled = request.IsEnabled
        };

        return await _maskingService.CreateRuleAsync(rule, cancellationToken);
    }
}

public class UpdateDataMaskingRuleHandler : IRequestHandler<UpdateDataMaskingRuleCommand, Result<DataMaskingRule>>
{
    private readonly IDataMaskingService _maskingService;

    public UpdateDataMaskingRuleHandler(IDataMaskingService maskingService)
    {
        _maskingService = maskingService;
    }

    public async Task<Result<DataMaskingRule>> Handle(
        UpdateDataMaskingRuleCommand request,
        CancellationToken cancellationToken)
    {
        var rule = new DataMaskingRule
        {
            Id = request.RuleId,
            Name = request.Name,
            Description = request.Description,
            ResourceType = request.ResourceType,
            FieldName = request.FieldName,
            MaskingType = request.MaskingType,
            MaskingPattern = request.MaskingPattern,
            ExemptUserIdsJson = request.ExemptUserIds != null ? System.Text.Json.JsonSerializer.Serialize(request.ExemptUserIds) : null,
            ExemptRoleIdsJson = request.ExemptRoleIds != null ? System.Text.Json.JsonSerializer.Serialize(request.ExemptRoleIds) : null,
            ExemptPermissionsJson = request.ExemptPermissions != null ? System.Text.Json.JsonSerializer.Serialize(request.ExemptPermissions) : null,
            IsEnabled = request.IsEnabled
        };

        return await _maskingService.UpdateRuleAsync(rule, cancellationToken);
    }
}

public class DeleteDataMaskingRuleHandler : IRequestHandler<DeleteDataMaskingRuleCommand, Result>
{
    private readonly IDataMaskingService _maskingService;

    public DeleteDataMaskingRuleHandler(IDataMaskingService maskingService)
    {
        _maskingService = maskingService;
    }

    public async Task<Result> Handle(
        DeleteDataMaskingRuleCommand request,
        CancellationToken cancellationToken)
    {
        return await _maskingService.DeleteRuleAsync(request.RuleId, cancellationToken);
    }
}

public class ApplyDataMaskingHandler : IRequestHandler<ApplyDataMaskingQuery, Result<MaskedFieldResult>>
{
    private readonly IDataMaskingService _maskingService;

    public ApplyDataMaskingHandler(IDataMaskingService maskingService)
    {
        _maskingService = maskingService;
    }

    public async Task<Result<MaskedFieldResult>> Handle(
        ApplyDataMaskingQuery request,
        CancellationToken cancellationToken)
    {
        return await _maskingService.ApplyMaskingAsync(
            request.ResourceType,
            request.FieldName,
            request.Value,
            request.UserId,
            request.TenantId,
            cancellationToken);
    }
}

public class CheckMaskingExemptionHandler : IRequestHandler<CheckMaskingExemptionQuery, Result<bool>>
{
    private readonly IDataMaskingService _maskingService;

    public CheckMaskingExemptionHandler(IDataMaskingService maskingService)
    {
        _maskingService = maskingService;
    }

    public async Task<Result<bool>> Handle(
        CheckMaskingExemptionQuery request,
        CancellationToken cancellationToken)
    {
        return await _maskingService.CanSeeUnmaskedAsync(request.RuleId, request.UserId, cancellationToken);
    }
}

public class GetDataMaskingRuleHandler : IRequestHandler<GetDataMaskingRuleQuery, Result<DataMaskingRule>>
{
    private readonly IDataMaskingService _maskingService;

    public GetDataMaskingRuleHandler(IDataMaskingService maskingService)
    {
        _maskingService = maskingService;
    }

    public async Task<Result<DataMaskingRule>> Handle(
        GetDataMaskingRuleQuery request,
        CancellationToken cancellationToken)
    {
        return await _maskingService.GetRuleAsync(request.RuleId, cancellationToken);
    }
}

public class ListDataMaskingRulesHandler : IRequestHandler<ListDataMaskingRulesQuery, Result<List<DataMaskingRule>>>
{
    private readonly IDataMaskingService _maskingService;

    public ListDataMaskingRulesHandler(IDataMaskingService maskingService)
    {
        _maskingService = maskingService;
    }

    public async Task<Result<List<DataMaskingRule>>> Handle(
        ListDataMaskingRulesQuery request,
        CancellationToken cancellationToken)
    {
        return await _maskingService.ListRulesAsync(request.TenantId, cancellationToken);
    }
}

public class GetDataAccessLogsHandler : IRequestHandler<GetDataAccessLogsQuery, Result<List<DataAccessLog>>>
{
    private readonly IDataMaskingService _maskingService;

    public GetDataAccessLogsHandler(IDataMaskingService maskingService)
    {
        _maskingService = maskingService;
    }

    public async Task<Result<List<DataAccessLog>>> Handle(
        GetDataAccessLogsQuery request,
        CancellationToken cancellationToken)
    {
        return await _maskingService.GetAccessLogsAsync(
            request.RuleId,
            request.UserId,
            request.FromDate,
            request.ToDate,
            cancellationToken);
    }
}

public class GetMaskingStatisticsHandler : IRequestHandler<GetMaskingStatisticsQuery, Result<MaskingStatistics>>
{
    private readonly IDataMaskingService _maskingService;

    public GetMaskingStatisticsHandler(IDataMaskingService maskingService)
    {
        _maskingService = maskingService;
    }

    public async Task<Result<MaskingStatistics>> Handle(
        GetMaskingStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        return await _maskingService.GetStatisticsAsync(request.RuleId, cancellationToken);
    }
}
