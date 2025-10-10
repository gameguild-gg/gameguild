namespace GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Resources.Entities;

/// <summary>
/// Service interface for ABAC (Attribute-Based Access Control) policy engine
/// Enables fine-grained access control based on attribute expressions
/// </summary>
public interface IAbacPolicyService
{
    /// <summary>
    /// Evaluate ABAC policies for a given context
    /// </summary>
    /// <param name="context">Evaluation context with user, resource, and environmental attributes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Evaluation result with decision and matched policies</returns>
    Task<AbacEvaluationResult> EvaluatePoliciesAsync(AbacEvaluationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new ABAC policy
    /// </summary>
    /// <param name="policy">Policy to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created policy</returns>
    Task<AbacPolicy> CreatePolicyAsync(AbacPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing ABAC policy
    /// </summary>
    /// <param name="policy">Policy to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated policy</returns>
    Task<AbacPolicy> UpdatePolicyAsync(AbacPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an ABAC policy
    /// </summary>
    /// <param name="policyId">Policy ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeletePolicyAsync(Guid policyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get policy by ID
    /// </summary>
    /// <param name="policyId">Policy ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Policy or null if not found</returns>
    Task<AbacPolicy?> GetPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all policies for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID (null for global policies)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of policies</returns>
    Task<List<AbacPolicy>> GetPoliciesByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get policies by resource type
    /// </summary>
    /// <param name="resourceType">Resource type</param>
    /// <param name="tenantId">Optional tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of policies</returns>
    Task<List<AbacPolicy>> GetPoliciesByResourceTypeAsync(string resourceType, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activate or deactivate a policy
    /// </summary>
    /// <param name="policyId">Policy ID</param>
    /// <param name="isActive">Active status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated policy</returns>
    Task<AbacPolicy> SetPolicyActiveStatusAsync(Guid policyId, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate policy expression syntax
    /// </summary>
    /// <param name="attributeExpression">Attribute expression JSON</param>
    /// <param name="conditionExpression">Optional condition expression</param>
    /// <returns>Validation result with errors if any</returns>
    Task<(bool IsValid, List<string> Errors)> ValidatePolicyExpressionAsync(string attributeExpression, string? conditionExpression = null);

    /// <summary>
    /// Clear ABAC policy cache
    /// </summary>
    Task ClearPolicyCacheAsync();
}
