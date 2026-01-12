using GameGuild.CQRS;

namespace GameGuild.Identity.Authorization.Queries;

// ============================================================================
// Permission Delegation Queries
// ============================================================================

/// <summary>
///     Query to get a delegation by ID
/// </summary>
public record GetDelegationByIdQuery(Guid DelegationId) : IQuery<PermissionDelegation?>;

public class GetDelegationByIdHandler(IPermissionDelegationService service)
    : IQueryHandler<GetDelegationByIdQuery, PermissionDelegation?>
{
    public async Task<PermissionDelegation?> Handle(
        GetDelegationByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetDelegationByIdAsync(request.DelegationId, cancellationToken);
    }
}

/// <summary>
///     Query to get active delegations for a delegate
/// </summary>
public record GetActiveDelegationsQuery(Guid DelegateUserId, Guid? TenantId) : IQuery<List<PermissionDelegation>>;

public class GetActiveDelegationsHandler(IPermissionDelegationService service)
    : IQueryHandler<GetActiveDelegationsQuery, List<PermissionDelegation>>
{
    public async Task<List<PermissionDelegation>> Handle(
        GetActiveDelegationsQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetActiveDelegationsAsync(request.DelegateUserId, request.TenantId, cancellationToken);
    }
}

/// <summary>
///     Query to get delegations made by a delegator
/// </summary>
public record GetDelegationsByDelegatorQuery(Guid DelegatorUserId, Guid? TenantId) : IQuery<List<PermissionDelegation>>;

public class GetDelegationsByDelegatorHandler(IPermissionDelegationService service)
    : IQueryHandler<GetDelegationsByDelegatorQuery, List<PermissionDelegation>>
{
    public async Task<List<PermissionDelegation>> Handle(
        GetDelegationsByDelegatorQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetDelegationsByDelegatorAsync(request.DelegatorUserId, request.TenantId, cancellationToken);
    }
}

/// <summary>
///     Query to check if a user has a delegated permission
/// </summary>
public record CheckDelegatedPermissionQuery(
    Guid DelegateUserId,
    string Permission,
    Guid? TenantId,
    Guid? ResourceId = null
) : IQuery<bool>;

public class CheckDelegatedPermissionHandler(IPermissionDelegationService service)
    : IQueryHandler<CheckDelegatedPermissionQuery, bool>
{
    public async Task<bool> Handle(
        CheckDelegatedPermissionQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.CheckDelegatedPermissionAsync(
            request.DelegateUserId,
            request.Permission,
            request.TenantId,
            request.ResourceId,
            cancellationToken
        );
    }
}
