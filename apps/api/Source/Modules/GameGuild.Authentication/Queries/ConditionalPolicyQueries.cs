using GameGuild.Authentication.DTOs;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Queries;

/// <summary>
///     Query to get conditional policy statistics
/// </summary>
public class GetConditionalPolicyStatisticsQuery : IRequest<ConditionalPolicyStatisticsDto>
{
    public Guid? TenantId { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }
}

/// <summary>
///     Query to get conditional policy usage information
/// </summary>
public class GetConditionalPolicyUsageQuery : IRequest<ConditionalPolicyUsageDto>
{
    public Guid PolicyId { get; set; }

    public Guid? TenantId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }
}

/// <summary>
///     Query to get conditional policy evaluation history
/// </summary>
public class GetConditionalPolicyEvaluationHistoryQuery : IRequest<ConditionalPolicyEvaluationHistoryDto>
{
    public Guid PolicyId { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }
}

/// <summary>
///     Query to get conditional policy conflicts
/// </summary>
public class GetConditionalPolicyConflictsQuery : IRequest<ConditionalPolicyConflictsDto>
{
    public Guid? TenantId { get; set; }
}

/// <summary>
///     Query to get conditional policy templates
/// </summary>
public class GetConditionalPolicyTemplatesQuery : IRequest<List<ConditionalPolicyTemplateDto>>
{
    public string? Category { get; set; }
}

/// <summary>
///     Query to get available policy condition types
/// </summary>
public class GetPolicyConditionTypesQuery : IRequest<List<PolicyConditionTypeDto>> { }
