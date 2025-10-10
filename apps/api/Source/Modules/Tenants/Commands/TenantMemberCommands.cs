using GameGuild.Core;
using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Command to add a member to a tenant
/// </summary>
public sealed record AddTenantMemberCommand(
    Guid UserId,
    Guid TenantId,
    string Role) : IRequest<Result<TenantMemberDto>>;

/// <summary>
///     Command to remove a member from a tenant
/// </summary>
public sealed record RemoveTenantMemberCommand(
    Guid UserId,
    Guid TenantId,
    string? LeaveReason = null) : IRequest<Result>;

/// <summary>
///     Command to update a member's role in a tenant
/// </summary>
public sealed record UpdateTenantMemberRoleCommand(
    Guid UserId,
    Guid TenantId,
    string NewRole) : IRequest<Result<TenantMemberDto>>;

/// <summary>
///     Command to activate a tenant member
/// </summary>
public sealed record ActivateTenantMemberCommand(
    Guid UserId,
    Guid TenantId) : IRequest<Result<TenantMemberDto>>;
