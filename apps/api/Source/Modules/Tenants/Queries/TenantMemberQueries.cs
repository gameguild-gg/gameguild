using GameGuild.Core;
using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Query to get all members of a tenant
/// </summary>
public sealed record GetTenantMembersQuery(
    Guid TenantId,
    bool ActiveOnly = false) : IRequest<Result<IReadOnlyList<TenantMemberDto>>>;

/// <summary>
///     Query to get all tenants a user is a member of
/// </summary>
public sealed record GetUserTenantsQuery(
    Guid UserId,
    bool ActiveOnly = false) : IRequest<Result<IReadOnlyList<TenantMemberDto>>>;

/// <summary>
///     Query to get a specific tenant member
/// </summary>
public sealed record GetTenantMemberQuery(
    Guid UserId,
    Guid TenantId) : IRequest<Result<TenantMemberDto>>;

/// <summary>
///     Query to check if a user is a member of a tenant
/// </summary>
public sealed record IsMemberOfTenantQuery(
    Guid UserId,
    Guid TenantId) : IRequest<Result<bool>>;
