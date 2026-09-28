namespace GameGuild.Identity.Authorization.Caching;

/// <summary>A resource access decision to precompute through the normal authorization cache path.</summary>
public sealed record PermissionCacheWarmupRequest(
    Guid TenantId,
    AclSubject Subject,
    string ResourceType,
    string ResourceId);

/// <summary>Counts work performed by an explicit permission-cache warmup request.</summary>
public sealed record PermissionCacheWarmupResult(int Requested, int Warmed, int DuplicatesSkipped);

/// <summary>Precomputes selected ACL decisions through the normal versioned cache path.</summary>
public interface IPermissionCacheWarmupService
{
    /// <summary>Evaluates up to 500 distinct subject/resource pairs and stores their results in L1/L2.</summary>
    Task<PermissionCacheWarmupResult> WarmAsync(
        IReadOnlyCollection<PermissionCacheWarmupRequest> requests,
        CancellationToken cancellationToken = default);
}

/// <summary>Uses the registered ACL service so warmup follows normal authorization and cache semantics.</summary>
public sealed class PermissionCacheWarmupService(IAccessControlListService accessControlListService)
    : IPermissionCacheWarmupService
{
    private const int MaxRequests = 500;
    private const int MaxPrincipalIds = 64;

    public async Task<PermissionCacheWarmupResult> WarmAsync(
        IReadOnlyCollection<PermissionCacheWarmupRequest> requests,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);
        if (requests.Count > MaxRequests)
            throw new ArgumentOutOfRangeException(nameof(requests), $"At most {MaxRequests} cache entries can be warmed at once.");

        var distinctRequests = new Dictionary<WarmupKey, PermissionCacheWarmupRequest>();
        foreach (var request in requests)
        {
            Validate(request);
            var (key, normalizedRequest) = WarmupKey.From(request);
            distinctRequests.TryAdd(key, normalizedRequest);
        }

        foreach (var request in distinctRequests.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await accessControlListService.EvaluateAccessAsync(
                    request.Subject,
                    request.TenantId,
                    request.ResourceType,
                    request.ResourceId,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return new PermissionCacheWarmupResult(requests.Count, distinctRequests.Count, requests.Count - distinctRequests.Count);
    }

    private static void Validate(PermissionCacheWarmupRequest? request)
    {
        if (request is null)
            throw new ArgumentException("Cache warmup entries cannot be null.", nameof(request));
        if (request.TenantId == Guid.Empty)
            throw new ArgumentException("A tenant ID is required for each cache warmup entry.", nameof(request));
        ArgumentNullException.ThrowIfNull(request.Subject);
        ArgumentNullException.ThrowIfNull(request.Subject.RoleIds);
        ArgumentNullException.ThrowIfNull(request.Subject.GroupIds);
        if (request.Subject.UserId == Guid.Empty)
            throw new ArgumentException("A subject user ID cannot be empty.", nameof(request));
        if (request.Subject.RoleIds.Count > MaxPrincipalIds || request.Subject.GroupIds.Count > MaxPrincipalIds ||
            request.Subject.RoleIds.Any(id => id == Guid.Empty) || request.Subject.GroupIds.Any(id => id == Guid.Empty))
            throw new ArgumentException($"Each subject can contain at most {MaxPrincipalIds} non-empty role and group IDs.", nameof(request));
        if (!request.Subject.IsAuthenticated && (request.Subject.UserId.HasValue ||
                                                 request.Subject.RoleIds.Count > 0 || request.Subject.GroupIds.Count > 0))
            throw new ArgumentException("Anonymous subjects cannot include user, role, or group identifiers.", nameof(request));
        if (request.Subject.IsAuthenticated && !request.Subject.UserId.HasValue)
            throw new ArgumentException("Authenticated subjects require a user ID.", nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ResourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ResourceId);
        if (request.ResourceType.Length > 128 || request.ResourceId.Length > 255)
            throw new ArgumentException("Resource type and ID must be at most 128 and 255 characters.", nameof(request));
    }

    private readonly record struct WarmupKey(
        Guid TenantId,
        bool IsAuthenticated,
        Guid? UserId,
        string RoleIds,
        string GroupIds,
        string ResourceType,
        string ResourceId)
    {
        public static (WarmupKey Key, PermissionCacheWarmupRequest Request) From(PermissionCacheWarmupRequest request)
        {
            var roleIds = request.Subject.RoleIds.Distinct().Order().ToArray();
            var groupIds = request.Subject.GroupIds.Distinct().Order().ToArray();
            var subject = request.Subject with
            {
                RoleIds = roleIds,
                GroupIds = groupIds
            };

            var key = new WarmupKey(
                request.TenantId,
                subject.IsAuthenticated,
                subject.UserId,
                string.Join(',', roleIds),
                string.Join(',', groupIds),
                request.ResourceType,
                request.ResourceId);
            return (key, request with { Subject = subject });
        }
    }
}
