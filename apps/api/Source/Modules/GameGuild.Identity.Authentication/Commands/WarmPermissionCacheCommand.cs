using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Caching;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authentication;

/// <summary>Precomputes selected ACL decisions for frequently used tenant resources.</summary>
public sealed record WarmPermissionCacheCommand : ICommand<PermissionCacheWarmupResult>
{
    public Guid TenantId { get; init; }

    public IReadOnlyCollection<WarmPermissionCacheItem> Items { get; init; } = [];
}

/// <summary>A user and resource pair to evaluate through the normal authorization cache path.</summary>
public sealed record WarmPermissionCacheItem
{
    public Guid? UserId { get; init; }

    public IReadOnlyCollection<Guid>? RoleIds { get; init; } = [];

    public IReadOnlyCollection<Guid>? GroupIds { get; init; } = [];

    public string ResourceType { get; init; } = string.Empty;

    public string ResourceId { get; init; } = string.Empty;
}

/// <summary>System-admin-only handler for explicit permission cache prewarming.</summary>
public sealed class WarmPermissionCacheCommandHandler(
    IActorContextAccessor actorContextAccessor,
    IPermissionCacheWarmupService warmupService)
    : ICommandHandler<WarmPermissionCacheCommand, PermissionCacheWarmupResult>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public Task<PermissionCacheWarmupResult> Handle(
        WarmPermissionCacheCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (!Actor.IsAuthenticated || !Actor.IsSystemAdmin)
            throw new UnauthorizedAccessException("Permission cache warmup requires system administration.");
        if (request.TenantId == Guid.Empty)
            throw new ArgumentException("A tenant ID is required for cache warmup.", nameof(request));
        ArgumentNullException.ThrowIfNull(request.Items);

        var warmupRequests = request.Items.Select(item =>
        {
            ArgumentNullException.ThrowIfNull(item);
            var roleIds = item.RoleIds ?? [];
            var groupIds = item.GroupIds ?? [];
            if (!item.UserId.HasValue && (roleIds.Count > 0 || groupIds.Count > 0))
                throw new ArgumentException("Anonymous warmup entries cannot include role or group IDs.", nameof(request));

            var subject = item.UserId.HasValue
                ? AclSubject.ForUser(item.UserId.Value, roleIds, groupIds)
                : AclSubject.Anonymous;
            return new PermissionCacheWarmupRequest(request.TenantId, subject, item.ResourceType, item.ResourceId);
        }).ToArray();

        return warmupService.WarmAsync(warmupRequests, cancellationToken);
    }
}
