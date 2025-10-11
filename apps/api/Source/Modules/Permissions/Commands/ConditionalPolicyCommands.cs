using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Constants;
using GameGuild.Modules.Permissions.Entities;
using GameGuild.Modules.Permissions.Abstractions;

namespace GameGuild.Modules.Permissions.Commands;

/// <summary>
/// Command to create a conditional policy
/// </summary>
public record CreateConditionalPolicyCommand(
    Guid? TenantId,
    string Name,
    string? Description,
    PolicyConditionType ConditionType,
    PermissionType? PermissionType,
    string? ResourceType,
    PolicyAction Action,
    int Priority,
    string? TimeConditions,
    string? EnvironmentConditions,
    string? LocationConditions,
    string? DeviceConditions,
    string? CustomConditions,
    string? EnforcementMessage,
    DateTime? EffectiveFrom,
    DateTime? EffectiveUntil,
    Guid CreatedBy
) : IRequest<Result<ConditionalPolicy>>;

/// <summary>
/// Command to update a conditional policy
/// </summary>
public record UpdateConditionalPolicyCommand(
    Guid PolicyId,
    string Name,
    string? Description,
    PolicyConditionType ConditionType,
    PermissionType? PermissionType,
    string? ResourceType,
    PolicyAction Action,
    int Priority,
    bool IsEnabled,
    string? TimeConditions,
    string? EnvironmentConditions,
    string? LocationConditions,
    string? DeviceConditions,
    string? CustomConditions,
    string? EnforcementMessage,
    DateTime? EffectiveFrom,
    DateTime? EffectiveUntil,
    Guid UpdatedBy
) : IRequest<Result<ConditionalPolicy>>;

/// <summary>
/// Command to delete a conditional policy
/// </summary>
public record DeleteConditionalPolicyCommand(
    Guid PolicyId
) : IRequest<Result>;

/// <summary>
/// Command to evaluate policies for a permission request
/// </summary>
public record EvaluateConditionalPoliciesCommand(
    Guid UserId,
    Guid? TenantId,
    PermissionType Permission,
    string? ResourceType,
    string? IpAddress,
    string? Country,
    string? Environment,
    string? DeviceType,
    bool IsDeviceCompliant,
    double? RiskScore
) : IRequest<Result<PolicyEvaluationResult>>;

/// <summary>
/// Query to get a conditional policy
/// </summary>
public record GetConditionalPolicyQuery(
    Guid PolicyId
) : IRequest<Result<ConditionalPolicy>>;

/// <summary>
/// Query to list conditional policies
/// </summary>
public record ListConditionalPoliciesQuery(
    Guid? TenantId,
    bool IncludeDisabled = false
) : IRequest<Result<List<ConditionalPolicy>>>;

/// <summary>
/// Command to test a policy against a context
/// </summary>
public record TestConditionalPolicyCommand(
    Guid PolicyId,
    DateTime RequestTime,
    string? IpAddress,
    string? Country,
    string? Environment,
    string? DeviceType,
    bool IsDeviceCompliant,
    double? RiskScore
) : IRequest<Result<PolicyTestResult>>;

/// <summary>
/// Query to get policy statistics
/// </summary>
public record GetPolicyStatisticsQuery(
    Guid? TenantId,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<Result<PolicyStatistics>>;
