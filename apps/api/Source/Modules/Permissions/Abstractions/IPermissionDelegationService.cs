using GameGuild.Modules.Permissions.Entities;

namespace GameGuild.Modules.Permissions.Abstractions;

/// <summary>
/// Service for managing permission delegations
/// </summary>
public interface IPermissionDelegationService
{
    /// <summary>
    /// Create a new permission delegation
    /// </summary>
    Task<PermissionDelegation> CreateDelegationAsync(
        Guid delegatorUserId,
        Guid delegateUserId,
        Guid? tenantId,
        Guid? resourceId,
        PermissionType[] permissions,
        DateTime? expiresAt = null,
        bool canSubDelegate = false,
        string? reason = null,
        int? usageLimit = null);

    /// <summary>
    /// Revoke an existing delegation
    /// </summary>
    Task RevokeDelegationAsync(Guid delegationId, Guid revokingUserId);

    /// <summary>
    /// Check if a user has a permission through delegation
    /// </summary>
    Task<bool> HasDelegatedPermissionAsync(
        Guid userId,
        Guid? tenantId,
        Guid? resourceId,
        PermissionType permission);

    /// <summary>
    /// Get all active delegations for a user (as delegate)
    /// </summary>
    Task<IEnumerable<PermissionDelegation>> GetUserDelegationsAsync(Guid userId);

    /// <summary>
    /// Get all delegations created by a user (as delegator)
    /// </summary>
    Task<IEnumerable<PermissionDelegation>> GetCreatedDelegationsAsync(Guid delegatorUserId);

    /// <summary>
    /// Get delegations for a specific tenant
    /// </summary>
    Task<IEnumerable<PermissionDelegation>> GetTenantDelegationsAsync(Guid tenantId);

    /// <summary>
    /// Get delegations for a specific resource
    /// </summary>
    Task<IEnumerable<PermissionDelegation>> GetResourceDelegationsAsync(Guid resourceId);

    /// <summary>
    /// Record usage of a delegation
    /// </summary>
    Task RecordDelegationUsageAsync(Guid delegationId);

    /// <summary>
    /// Clean up expired delegations
    /// </summary>
    Task CleanupExpiredDelegationsAsync();

    /// <summary>
    /// Check if user can delegate specific permissions
    /// </summary>
    Task<bool> CanDelegatePermissionsAsync(
        Guid delegatorUserId,
        Guid? tenantId,
        Guid? resourceId,
        PermissionType[] permissions);
}