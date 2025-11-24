using GameGuild.Authentication.Entities;
using GameGuild.Authentication.Enums;

namespace GameGuild.Authentication.Abstractions;

/// <summary>
///     Service interface for conditional policy management
///     Handles time, location, environment, and context-based access control
/// </summary>
public interface IConditionalPolicyService
{
    /// <summary>
    ///     Evaluate conditional policies for a permission request
    /// </summary>
    /// <param name="userId">User ID making the request</param>
    /// <param name="tenantId">Tenant ID context</param>
    /// <param name="permission">Permission being requested</param>
    /// <param name="resourceType">Type of resource (optional)</param>
    /// <param name="contextInfo">Additional context information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Policy evaluation result</returns>
    Task<ConditionalPolicyResult> EvaluatePoliciesAsync(
        Guid userId,
        Guid? tenantId,
        PermissionType permission,
        string? resourceType = null,
        ConditionalPolicyContext? contextInfo = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Create a new conditional policy
    /// </summary>
    /// <param name="policy">Policy to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created policy</returns>
    Task<ConditionalPolicy> CreatePolicyAsync(ConditionalPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update an existing conditional policy
    /// </summary>
    /// <param name="policy">Policy to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated policy</returns>
    Task<ConditionalPolicy> UpdatePolicyAsync(ConditionalPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a conditional policy
    /// </summary>
    /// <param name="policyId">Policy ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeletePolicyAsync(Guid policyId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get policy by ID
    /// </summary>
    /// <param name="policyId">Policy ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Policy or null if not found</returns>
    Task<ConditionalPolicy?> GetPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all policies for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID (null for global policies)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of policies</returns>
    Task<List<ConditionalPolicy>> GetPoliciesByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get policies by condition type
    /// </summary>
    /// <param name="conditionType">Condition type</param>
    /// <param name="tenantId">Optional tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of policies</returns>
    Task<List<ConditionalPolicy>> GetPoliciesByConditionTypeAsync(PolicyConditionType conditionType, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Enable or disable a policy
    /// </summary>
    /// <param name="policyId">Policy ID</param>
    /// <param name="isEnabled">Enabled status</param>
    /// <param name="updatedBy">User ID who is updating the policy</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated policy</returns>
    Task<ConditionalPolicy> SetPolicyEnabledStatusAsync(Guid policyId, bool isEnabled, Guid updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Test a policy against specific conditions
    /// </summary>
    /// <param name="policyId">Policy ID to test</param>
    /// <param name="testContext">Test context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result</returns>
    Task<ConditionalPolicyResult> TestPolicyAsync(Guid policyId, ConditionalPolicyContext testContext, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get policy enforcement statistics
    /// </summary>
    /// <param name="tenantId">Optional tenant ID</param>
    /// <param name="from">Optional start date</param>
    /// <param name="to">Optional end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Policy statistics</returns>
    Task<ConditionalPolicyStatistics> GetPolicyStatisticsAsync(Guid? tenantId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}
