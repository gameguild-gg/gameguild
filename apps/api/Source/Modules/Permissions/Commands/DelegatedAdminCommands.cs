using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Constants;
using GameGuild.Modules.Permissions.Entities;
using GameGuild.Modules.Permissions.Abstractions;

namespace GameGuild.Modules.Permissions.Commands;

/// <summary>
/// Create a new delegated administration scope
/// </summary>
public record CreateDelegatedAdminCommand(
    Guid DelegatorUserId,
    Guid DelegatedUserId,
    Guid TenantId,
    string ScopeType,
    Guid? ScopeId,
    string ScopeName,
    PermissionType[] Permissions,
    bool AllowSubDelegation = false,
    int MaxDelegationDepth = 0,
    DateTime? ExpiresAt = null,
    string? Reason = null,
    Dictionary<string, object>? Constraints = null
) : IRequest<Result<DelegatedAdminScope>>;

/// <summary>
/// Create a sub-delegation from an existing delegation
/// </summary>
public record CreateSubDelegationCommand(
    Guid ParentDelegationId,
    Guid NewDelegatedUserId,
    PermissionType[] Permissions,
    DateTime? ExpiresAt = null,
    string? Reason = null
) : IRequest<Result<DelegatedAdminScope>>;

/// <summary>
/// Revoke a delegated administration scope
/// </summary>
public record RevokeDelegationCommand(
    Guid DelegationId,
    Guid RevokedByUserId,
    string Reason,
    bool RevokeSubDelegations = true
) : IRequest<Result>;

/// <summary>
/// Get user's delegated permissions in a scope
/// </summary>
public record GetUserDelegatedPermissionsQuery(
    Guid UserId,
    Guid TenantId,
    string ScopeType,
    Guid? ScopeId
) : IRequest<Result<PermissionType[]>>;

/// <summary>
/// Check if user has delegated permission
/// </summary>
public record CheckDelegatedPermissionQuery(
    Guid UserId,
    Guid TenantId,
    string ScopeType,
    Guid? ScopeId,
    PermissionType Permission
) : IRequest<Result<bool>>;

/// <summary>
/// Get delegation statistics for a tenant
/// </summary>
public record GetDelegationStatisticsQuery(
    Guid TenantId
) : IRequest<Result<DelegationStatistics>>;
