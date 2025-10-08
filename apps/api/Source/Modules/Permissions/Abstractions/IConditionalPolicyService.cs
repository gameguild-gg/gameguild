using GameGuild.Modules.Permissions.Constants;
using GameGuild.Modules.Permissions.Entities;
using GameGuild.Shared;

namespace GameGuild.Modules.Permissions.Abstractions;

/// <summary>
/// Service for managing and evaluating conditional policies
/// </summary>
public interface IConditionalPolicyService
{
    /// <summary>
    /// Evaluates all applicable conditional policies for a permission request
    /// </summary>
    Task<Result<PolicyEvaluationResult>> EvaluatePoliciesAsync(
        Guid userId,
        Guid? tenantId,
        PermissionType permission,
        string? resourceType,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new conditional policy
    /// </summary>
    Task<Result<ConditionalPolicy>> CreatePolicyAsync(
        ConditionalPolicy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing conditional policy
    /// </summary>
    Task<Result<ConditionalPolicy>> UpdatePolicyAsync(
        ConditionalPolicy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a conditional policy
    /// </summary>
    Task<Result> DeletePolicyAsync(
        Guid policyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a conditional policy by ID
    /// </summary>
    Task<Result<ConditionalPolicy>> GetPolicyAsync(
        Guid policyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all policies for a tenant (or global if tenantId is null)
    /// </summary>
    Task<Result<List<ConditionalPolicy>>> ListPoliciesAsync(
        Guid? tenantId,
        bool includeDisabled = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests a policy against a specific context without actually enforcing it
    /// </summary>
    Task<Result<PolicyTestResult>> TestPolicyAsync(
        Guid policyId,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets policy evaluation statistics
    /// </summary>
    Task<Result<PolicyStatistics>> GetStatisticsAsync(
        Guid? tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Context information for policy evaluation
/// </summary>
public class PolicyEvaluationContext
{
    public DateTime RequestTime { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? Environment { get; set; }
    public string? DeviceType { get; set; }
    public string? UserAgent { get; set; }
    public bool IsDeviceCompliant { get; set; }
    public bool IsDeviceEncrypted { get; set; }
    public double? RiskScore { get; set; }
    public Dictionary<string, object>? CustomAttributes { get; set; }
}

/// <summary>
/// Result of policy evaluation
/// </summary>
public class PolicyEvaluationResult
{
    public PolicyDecision Decision { get; set; }
    public List<MatchedPolicy> MatchedPolicies { get; set; } = new();
    public string? Message { get; set; }
    public bool Require2FA { get; set; }
    public bool RequireApproval { get; set; }
    public List<string>? ApproverIds { get; set; }
}

/// <summary>
/// Final decision after policy evaluation
/// </summary>
public enum PolicyDecision
{
    Allow = 1,
    Deny = 2,
    Conditional = 3 // Requires additional steps like 2FA or approval
}

/// <summary>
/// Information about a policy that matched during evaluation
/// </summary>
public class MatchedPolicy
{
    public Guid PolicyId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public PolicyAction Action { get; set; }
    public int Priority { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Result of testing a policy
/// </summary>
public class PolicyTestResult
{
    public bool Matches { get; set; }
    public PolicyAction Action { get; set; }
    public List<string> MatchedConditions { get; set; } = new();
    public List<string> UnmatchedConditions { get; set; } = new();
    public string? Message { get; set; }
}

/// <summary>
/// Statistics about policy evaluations
/// </summary>
public class PolicyStatistics
{
    public int TotalPolicies { get; set; }
    public int EnabledPolicies { get; set; }
    public long TotalEvaluations { get; set; }
    public long AllowedByPolicy { get; set; }
    public long DeniedByPolicy { get; set; }
    public long ConditionalActions { get; set; }
    public Dictionary<string, int> PolicyHitCounts { get; set; } = new();
    public Dictionary<PolicyConditionType, int> ConditionTypeCounts { get; set; } = new();
}
