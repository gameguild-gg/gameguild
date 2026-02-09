using GameGuild.CQRS;

namespace GameGuild.Identity.Authorization.Queries;

// ============================================================================
// Delegated Admin Queries
// ============================================================================

/// <summary>
///     Query to get a delegated admin scope by ID
/// </summary>
public sealed record GetDelegatedAdminScopeByIdQuery(Guid ScopeId) : IQuery<DelegatedAdminScope?>;

public sealed class GetDelegatedAdminScopeByIdHandler(IDelegatedAdminService service)
    : IQueryHandler<GetDelegatedAdminScopeByIdQuery, DelegatedAdminScope?>
{
    public async Task<DelegatedAdminScope?> Handle(
        GetDelegatedAdminScopeByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetScopeByIdAsync(request.ScopeId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to get admin scopes for a user
/// </summary>
public sealed record GetAdminScopesQuery(Guid AdminUserId, Guid? TenantId) : IQuery<List<DelegatedAdminScope>>;

public sealed class GetAdminScopesHandler(IDelegatedAdminService service)
    : IQueryHandler<GetAdminScopesQuery, List<DelegatedAdminScope>>
{
    public async Task<List<DelegatedAdminScope>> Handle(
        GetAdminScopesQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetAdminScopesAsync(request.AdminUserId, request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to get managed users for an admin
/// </summary>
public sealed record GetManagedUsersQuery(Guid AdminUserId, Guid? TenantId) : IQuery<List<Guid>>;

public sealed class GetManagedUsersHandler(IDelegatedAdminService service)
    : IQueryHandler<GetManagedUsersQuery, List<Guid>>
{
    public async Task<List<Guid>> Handle(
        GetManagedUsersQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetManagedUsersAsync(request.AdminUserId, request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to get managed resource types for an admin
/// </summary>
public sealed record GetManagedResourceTypesQuery(Guid AdminUserId, Guid? TenantId) : IQuery<List<string>>;

public sealed class GetManagedResourceTypesHandler(IDelegatedAdminService service)
    : IQueryHandler<GetManagedResourceTypesQuery, List<string>>
{
    public async Task<List<string>> Handle(
        GetManagedResourceTypesQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetManagedResourceTypesAsync(request.AdminUserId, request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to check if admin can manage a user
/// </summary>
public sealed record CanManageUserQuery(Guid AdminUserId, Guid TargetUserId, Guid? TenantId) : IQuery<bool>;

public sealed class CanManageUserHandler(IDelegatedAdminService service)
    : IQueryHandler<CanManageUserQuery, bool>
{
    public async Task<bool> Handle(
        CanManageUserQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.CanManageUserAsync(
            request.AdminUserId,
            request.TargetUserId,
            request.TenantId,
            cancellationToken
        ).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to check if admin can manage a resource type
/// </summary>
public sealed record CanManageResourceQuery(Guid AdminUserId, string ResourceType, Guid? TenantId) : IQuery<bool>;

public sealed class CanManageResourceHandler(IDelegatedAdminService service)
    : IQueryHandler<CanManageResourceQuery, bool>
{
    public async Task<bool> Handle(
        CanManageResourceQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.CanManageResourceAsync(
            request.AdminUserId,
            request.ResourceType,
            request.TenantId,
            cancellationToken
        ).ConfigureAwait(false);
    }
}
