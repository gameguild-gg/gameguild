using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Commands;
using GameGuild.Modules.Permissions.Entities;
using GameGuild.Shared;

namespace GameGuild.Modules.Permissions.Handlers;

public class CreateConditionalPolicyHandler : IRequestHandler<CreateConditionalPolicyCommand, Result<ConditionalPolicy>>
{
    private readonly IConditionalPolicyService _policyService;

    public CreateConditionalPolicyHandler(IConditionalPolicyService policyService)
    {
        _policyService = policyService;
    }

    public async Task<Result<ConditionalPolicy>> Handle(
        CreateConditionalPolicyCommand request,
        CancellationToken cancellationToken)
    {
        var policy = new ConditionalPolicy
        {
            TenantId = request.TenantId,
            Name = request.Name,
            Description = request.Description,
            ConditionType = request.ConditionType,
            PermissionType = request.PermissionType,
            ResourceType = request.ResourceType,
            Action = request.Action,
            Priority = request.Priority,
            TimeConditions = request.TimeConditions,
            EnvironmentConditions = request.EnvironmentConditions,
            LocationConditions = request.LocationConditions,
            DeviceConditions = request.DeviceConditions,
            CustomConditions = request.CustomConditions,
            EnforcementMessage = request.EnforcementMessage,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveUntil = request.EffectiveUntil,
            CreatedBy = request.CreatedBy
        };

        return await _policyService.CreatePolicyAsync(policy, cancellationToken);
    }
}

public class UpdateConditionalPolicyHandler : IRequestHandler<UpdateConditionalPolicyCommand, Result<ConditionalPolicy>>
{
    private readonly IConditionalPolicyService _policyService;

    public UpdateConditionalPolicyHandler(IConditionalPolicyService policyService)
    {
        _policyService = policyService;
    }

    public async Task<Result<ConditionalPolicy>> Handle(
        UpdateConditionalPolicyCommand request,
        CancellationToken cancellationToken)
    {
        var policy = new ConditionalPolicy
        {
            Id = request.PolicyId,
            Name = request.Name,
            Description = request.Description,
            ConditionType = request.ConditionType,
            PermissionType = request.PermissionType,
            ResourceType = request.ResourceType,
            Action = request.Action,
            Priority = request.Priority,
            IsEnabled = request.IsEnabled,
            TimeConditions = request.TimeConditions,
            EnvironmentConditions = request.EnvironmentConditions,
            LocationConditions = request.LocationConditions,
            DeviceConditions = request.DeviceConditions,
            CustomConditions = request.CustomConditions,
            EnforcementMessage = request.EnforcementMessage,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveUntil = request.EffectiveUntil,
            UpdatedBy = request.UpdatedBy
        };

        return await _policyService.UpdatePolicyAsync(policy, cancellationToken);
    }
}

public class DeleteConditionalPolicyHandler : IRequestHandler<DeleteConditionalPolicyCommand, Result>
{
    private readonly IConditionalPolicyService _policyService;

    public DeleteConditionalPolicyHandler(IConditionalPolicyService policyService)
    {
        _policyService = policyService;
    }

    public async Task<Result> Handle(
        DeleteConditionalPolicyCommand request,
        CancellationToken cancellationToken)
    {
        return await _policyService.DeletePolicyAsync(request.PolicyId, cancellationToken);
    }
}

public class EvaluateConditionalPoliciesHandler : IRequestHandler<EvaluateConditionalPoliciesCommand, Result<PolicyEvaluationResult>>
{
    private readonly IConditionalPolicyService _policyService;

    public EvaluateConditionalPoliciesHandler(IConditionalPolicyService policyService)
    {
        _policyService = policyService;
    }

    public async Task<Result<PolicyEvaluationResult>> Handle(
        EvaluateConditionalPoliciesCommand request,
        CancellationToken cancellationToken)
    {
        var context = new PolicyEvaluationContext
        {
            RequestTime = DateTime.UtcNow,
            IpAddress = request.IpAddress,
            Country = request.Country,
            Environment = request.Environment,
            DeviceType = request.DeviceType,
            IsDeviceCompliant = request.IsDeviceCompliant,
            RiskScore = request.RiskScore
        };

        return await _policyService.EvaluatePoliciesAsync(
            request.UserId,
            request.TenantId,
            request.Permission,
            request.ResourceType,
            context,
            cancellationToken);
    }
}

public class GetConditionalPolicyHandler : IRequestHandler<GetConditionalPolicyQuery, Result<ConditionalPolicy>>
{
    private readonly IConditionalPolicyService _policyService;

    public GetConditionalPolicyHandler(IConditionalPolicyService policyService)
    {
        _policyService = policyService;
    }

    public async Task<Result<ConditionalPolicy>> Handle(
        GetConditionalPolicyQuery request,
        CancellationToken cancellationToken)
    {
        return await _policyService.GetPolicyAsync(request.PolicyId, cancellationToken);
    }
}

public class ListConditionalPoliciesHandler : IRequestHandler<ListConditionalPoliciesQuery, Result<List<ConditionalPolicy>>>
{
    private readonly IConditionalPolicyService _policyService;

    public ListConditionalPoliciesHandler(IConditionalPolicyService policyService)
    {
        _policyService = policyService;
    }

    public async Task<Result<List<ConditionalPolicy>>> Handle(
        ListConditionalPoliciesQuery request,
        CancellationToken cancellationToken)
    {
        return await _policyService.ListPoliciesAsync(
            request.TenantId,
            request.IncludeDisabled,
            cancellationToken);
    }
}

public class TestConditionalPolicyHandler : IRequestHandler<TestConditionalPolicyCommand, Result<PolicyTestResult>>
{
    private readonly IConditionalPolicyService _policyService;

    public TestConditionalPolicyHandler(IConditionalPolicyService policyService)
    {
        _policyService = policyService;
    }

    public async Task<Result<PolicyTestResult>> Handle(
        TestConditionalPolicyCommand request,
        CancellationToken cancellationToken)
    {
        var context = new PolicyEvaluationContext
        {
            RequestTime = request.RequestTime,
            IpAddress = request.IpAddress,
            Country = request.Country,
            Environment = request.Environment,
            DeviceType = request.DeviceType,
            IsDeviceCompliant = request.IsDeviceCompliant,
            RiskScore = request.RiskScore
        };

        return await _policyService.TestPolicyAsync(request.PolicyId, context, cancellationToken);
    }
}

public class GetPolicyStatisticsHandler : IRequestHandler<GetPolicyStatisticsQuery, Result<PolicyStatistics>>
{
    private readonly IConditionalPolicyService _policyService;

    public GetPolicyStatisticsHandler(IConditionalPolicyService policyService)
    {
        _policyService = policyService;
    }

    public async Task<Result<PolicyStatistics>> Handle(
        GetPolicyStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        return await _policyService.GetStatisticsAsync(
            request.TenantId,
            request.FromDate,
            request.ToDate,
            cancellationToken);
    }
}
