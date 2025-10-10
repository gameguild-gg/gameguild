using GameGuild.Modules.Permissions.Entities;
using GameGuild.Modules.Permissions.Constants;

namespace GameGuild.Modules.Permissions.Abstractions;

/// <summary>
/// Service for managing delegated administrative scopes
/// </summary>
public interface IDelegatedAdminService
{
    /// <summary>
    /// Create a new delegated administration scope
    /// </summary>
    Task<DelegatedAdminScope> CreateDelegationAsync(
        Guid delegatorUserId,
        Guid delegatedUserId,
        Guid tenantId,
        string scopeType,
        Guid? scopeId,
        string scopeName,
        PermissionType[] permissions,
        bool allowSubDelegation = false,
        int maxDelegationDepth = 0,
        DateTime? expiresAt = null,
        string? reason = null,
        Dictionary<string, object>? constraints = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a sub-delegation from an existing delegation
    /// </summary>
    Task<DelegatedAdminScope> CreateSubDelegationAsync(
        Guid parentDelegationId,
        Guid newDelegatedUserId,
        PermissionType[] permissions,
        DateTime? expiresAt = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a delegation
    /// </summary>
    Task RevokeDelegationAsync(
        Guid delegationId,
        Guid revokedByUserId,
        string reason,
        bool revokeSubDelegations = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active delegations for a user (where they are the delegated user)
    /// </summary>
    Task<IEnumerable<DelegatedAdminScope>> GetUserDelegationsAsync(
        Guid userId,
        Guid? tenantId = null,
        bool includeExpired = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all delegations created by a user (where they are the delegator)
    /// </summary>
    Task<IEnumerable<DelegatedAdminScope>> GetDelegationsByDelegatorAsync(
        Guid delegatorUserId,
        Guid? tenantId = null,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user has a specific delegated permission in a scope
    /// </summary>
    Task<bool> HasDelegatedPermissionAsync(
        Guid userId,
        Guid tenantId,
        string scopeType,
        Guid? scopeId,
        PermissionType permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all permissions a user has through delegation in a specific scope
    /// </summary>
    Task<PermissionType[]> GetDelegatedPermissionsAsync(
        Guid userId,
        Guid tenantId,
        string scopeType,
        Guid? scopeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get delegation chain (parent delegations)
    /// </summary>
    Task<IEnumerable<DelegatedAdminScope>> GetDelegationChainAsync(
        Guid delegationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all sub-delegations (children) of a delegation
    /// </summary>
    Task<IEnumerable<DelegatedAdminScope>> GetSubDelegationsAsync(
        Guid parentDelegationId,
        bool recursive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activate a delegation
    /// </summary>
    Task ActivateDelegationAsync(Guid delegationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate a delegation
    /// </summary>
    Task DeactivateDelegationAsync(Guid delegationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get delegation by ID
    /// </summary>
    Task<DelegatedAdminScope?> GetDelegationByIdAsync(Guid delegationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update delegation expiration
    /// </summary>
    Task UpdateDelegationExpirationAsync(
        Guid delegationId,
        DateTime? newExpiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get delegation statistics for a tenant
    /// </summary>
    Task<DelegationStatistics> GetDelegationStatisticsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-revoke expired delegations (background job)
    /// </summary>
    Task AutoRevokeExpiredDelegationsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about delegations in a tenant
/// </summary>
public class DelegationStatistics
{
    public int TotalDelegations { get; set; }
    public int ActiveDelegations { get; set; }
    public int ExpiredDelegations { get; set; }
    public int RevokedDelegations { get; set; }
    public int SubDelegations { get; set; }
    public Dictionary<string, int> DelegationsByScopeType { get; set; } = new();
    public Dictionary<string, int> DelegationsByPermission { get; set; } = new();
    public double AverageDelegationDuration { get; set; }
}
