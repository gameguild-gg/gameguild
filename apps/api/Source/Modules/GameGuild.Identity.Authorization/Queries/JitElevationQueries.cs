using GameGuild.CQRS;

namespace GameGuild.Identity.Authorization.Queries;

// ============================================================================
// JIT Elevation Queries
// ============================================================================

/// <summary>
///     Query to get a JIT elevation request by ID
/// </summary>
public sealed record GetJitElevationByIdQuery(Guid RequestId) : IQuery<JitElevationRequest?>;

public sealed class GetJitElevationByIdHandler(IJitElevationService service)
    : IQueryHandler<GetJitElevationByIdQuery, JitElevationRequest?>
{
    public async Task<JitElevationRequest?> Handle(
        GetJitElevationByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetRequestByIdAsync(request.RequestId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to get pending JIT elevation requests
/// </summary>
public sealed record GetPendingJitElevationsQuery(Guid? TenantId) : IQuery<List<JitElevationRequest>>;

public sealed class GetPendingJitElevationsHandler(IJitElevationService service)
    : IQueryHandler<GetPendingJitElevationsQuery, List<JitElevationRequest>>
{
    public async Task<List<JitElevationRequest>> Handle(
        GetPendingJitElevationsQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetPendingRequestsAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to get user's JIT elevation requests
/// </summary>
public sealed record GetUserJitElevationsQuery(Guid UserId, Guid? TenantId) : IQuery<List<JitElevationRequest>>;

public sealed class GetUserJitElevationsHandler(IJitElevationService service)
    : IQueryHandler<GetUserJitElevationsQuery, List<JitElevationRequest>>
{
    public async Task<List<JitElevationRequest>> Handle(
        GetUserJitElevationsQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetUserRequestsAsync(request.UserId, request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to get active JIT elevations for a user
/// </summary>
public sealed record GetActiveJitElevationsQuery(Guid UserId, Guid? TenantId) : IQuery<List<JitElevationRequest>>;

public sealed class GetActiveJitElevationsHandler(IJitElevationService service)
    : IQueryHandler<GetActiveJitElevationsQuery, List<JitElevationRequest>>
{
    public async Task<List<JitElevationRequest>> Handle(
        GetActiveJitElevationsQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetActiveElevationsAsync(request.UserId, request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to check if user has an active JIT elevation for a permission
/// </summary>
public sealed record HasActiveJitElevationQuery(
    Guid UserId,
    string Permission,
    Guid? TenantId,
    Guid? ResourceId = null
) : IQuery<bool>;

public sealed class HasActiveJitElevationHandler(IJitElevationService service)
    : IQueryHandler<HasActiveJitElevationQuery, bool>
{
    public async Task<bool> Handle(
        HasActiveJitElevationQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.HasActiveElevationAsync(
            request.UserId,
            request.Permission,
            request.TenantId,
            request.ResourceId,
            cancellationToken
        );
    }
}
