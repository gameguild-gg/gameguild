using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Backward-compatible three-layer permission facade used by legacy authentication surfaces.
///     New request handlers should prefer the focused Authorization module services.
/// </summary>
/// <remarks>
///     <para>
///         <b>SECURITY:</b> every mutation path bumps the tenant security version (cache
///         invalidation) and writes a permission audit entry. The invalidation/audit
///         collaborators are optional so legacy construction sites keep working, but the
///         composition root always supplies them; when they are absent the mutation still
///         throws on failure to keep the fail-loud behavior of the Authorization module.
///     </para>
/// </remarks>
public class PermissionService(
    IApplicationDbContext context,
    ITenantSecurityVersionStore? securityVersionStore = null,
    IPermissionAuditService? auditService = null,
    IActorContextAccessor? actorContextAccessor = null) : IPermissionService
{
    private const int MaximumBulkPermissionCheckBatchSize = 256;

    public async Task<TenantPermission> GrantTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType[] permissions)
    {
        var grant = await GetTenantGrantAsync(userId, tenantId).ConfigureAwait(false);
        if (grant == null)
        {
            grant = new TenantPermission
            {
                UserId = userId,
                TenantId = tenantId,
                GrantedAt = SystemClock.UtcNow
            };
            context.Set<TenantPermission>().Add(grant);
        }

        grant.AddPermissions(ToPermissionNames(permissions));
        grant.IsActive = true;
        grant.ExpiresAt = null;

        await context.SaveChangesAsync().ConfigureAwait(false);

        await TrackMutationAsync(
            PermissionOperationType.Grant,
            userId,
            tenantId,
            ToPermissionNames(permissions)).ConfigureAwait(false);

        return grant;
    }

    public async Task<List<TenantPermission>> BulkGrantTenantPermissionAsync(Guid[] userIds, Guid tenantId, PermissionType[] permissions)
    {
        var results = new List<TenantPermission>();
        foreach (var userId in userIds.Where(id => id != Guid.Empty).Distinct())
        {
            results.Add(await GrantTenantPermissionAsync(userId, tenantId, permissions).ConfigureAwait(false));
        }

        return results;
    }

    public async Task<bool> HasTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType permission)
        => (await GetEffectiveTenantPermissionsAsync(userId, tenantId).ConfigureAwait(false)).Contains(permission);

    public async Task<IEnumerable<PermissionType>> GetTenantPermissionsAsync(Guid? userId, Guid? tenantId)
    {
        var grant = await GetTenantGrantAsync(userId, tenantId).ConfigureAwait(false);
        return grant == null || grant.IsExpired() || !grant.IsActive
            ? []
            : ToPermissionTypes(grant.Permissions);
    }

    public async Task<IEnumerable<PermissionType>> GetGlobalDefaultPermissionsAsync()
        => await GetTenantPermissionsAsync(null, null).ConfigureAwait(false);

    public async Task SetGlobalDefaultPermissionsAsync(PermissionType[] permissions)
    {
        await SetTenantGrantAsync(null, null, permissions, "Global default permissions").ConfigureAwait(false);
    }

    public async Task<IEnumerable<PermissionType>> GetTenantDefaultPermissionsAsync(Guid tenantId)
        => await GetTenantPermissionsAsync(null, tenantId).ConfigureAwait(false);

    public async Task SetTenantDefaultPermissionsAsync(Guid tenantId, PermissionType[] permissions)
    {
        await SetTenantGrantAsync(null, tenantId, permissions, "Tenant default permissions").ConfigureAwait(false);
    }

    public async Task RevokeTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType[] permissions)
    {
        var grant = await GetTenantGrantAsync(userId, tenantId).ConfigureAwait(false);
        if (grant == null) return;

        grant.RemovePermissions(ToPermissionNames(permissions));
        if (grant.Permissions.Length == 0)
        {
            context.Set<TenantPermission>().Remove(grant);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);

        await TrackMutationAsync(
            PermissionOperationType.Revoke,
            userId,
            tenantId,
            ToPermissionNames(permissions)).ConfigureAwait(false);
    }

    public async Task<TenantPermission> JoinTenantAsync(Guid userId, Guid tenantId)
    {
        var defaults = (await GetTenantDefaultPermissionsAsync(tenantId).ConfigureAwait(false)).ToArray();
        if (defaults.Length == 0)
        {
            defaults = [PermissionType.Read];
        }

        return await GrantTenantPermissionAsync(userId, tenantId, defaults).ConfigureAwait(false);
    }

    public async Task LeaveTenantAsync(Guid userId, Guid tenantId)
    {
        var directPermissions = (await GetTenantPermissionsAsync(userId, tenantId).ConfigureAwait(false)).ToArray();
        if (directPermissions.Length > 0)
        {
            await RevokeTenantPermissionAsync(userId, tenantId, directPermissions).ConfigureAwait(false);
        }
    }

    public async Task<bool> IsUserInTenantAsync(Guid userId, Guid tenantId)
    {
        var grant = await GetTenantGrantAsync(userId, tenantId).ConfigureAwait(false);
        return grant is { IsActive: true } && !grant.IsExpired();
    }

    public async Task<IEnumerable<TenantPermission>> GetUserTenantsAsync(Guid userId)
    {
        var grants = await context.Set<TenantPermission>()
            .Where(p => p.UserId == userId && p.TenantId != null && p.IsActive)
            .ToListAsync()
            .ConfigureAwait(false);

        return grants.Where(p => !p.IsExpired());
    }

    public async Task<IEnumerable<PermissionType>> GetEffectiveTenantPermissionsAsync(Guid? userId, Guid? tenantId)
    {
        var permissions = new HashSet<PermissionType>();
        AddRange(permissions, await GetGlobalDefaultPermissionsAsync().ConfigureAwait(false));

        if (tenantId.HasValue)
        {
            AddRange(permissions, await GetTenantDefaultPermissionsAsync(tenantId.Value).ConfigureAwait(false));
        }

        if (userId.HasValue)
        {
            AddRange(permissions, await GetTenantPermissionsAsync(userId, tenantId).ConfigureAwait(false));
        }

        return permissions;
    }

    public async Task<ContentTypePermission> GrantContentTypePermissionAsync(Guid? userId, Guid? tenantId, string contentTypeName, PermissionType[] permissions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentTypeName);

        var grant = await GetContentTypeGrantAsync(userId, tenantId, contentTypeName).ConfigureAwait(false);
        if (grant == null)
        {
            grant = new ContentTypePermission(userId, tenantId, contentTypeName);
            context.Set<ContentTypePermission>().Add(grant);
        }

        grant.AddPermissions(permissions);
        grant.IsActive = true;
        grant.ExpiresAt = null;

        await context.SaveChangesAsync().ConfigureAwait(false);

        await TrackMutationAsync(
            PermissionOperationType.Grant,
            userId,
            tenantId,
            permissions.Select(p => p.ToString()),
            contentTypeName).ConfigureAwait(false);

        return grant;
    }

    public async Task<bool> HasContentTypePermissionAsync(Guid? userId, Guid? tenantId, string contentTypeName, PermissionType permission)
        => (await GetEffectiveContentTypePermissionsAsync(userId, tenantId, contentTypeName).ConfigureAwait(false)).Contains(permission);

    public async Task<IEnumerable<PermissionType>> GetContentTypePermissionsAsync(Guid? userId, Guid? tenantId, string contentTypeName)
    {
        var grant = await GetContentTypeGrantAsync(userId, tenantId, contentTypeName).ConfigureAwait(false);
        return grant == null || !grant.IsEffective()
            ? []
            : grant.GetPermissionsAsEnum();
    }

    public async Task RevokeContentTypePermissionAsync(Guid? userId, Guid? tenantId, string contentTypeName, PermissionType[] permissions)
    {
        var grant = await GetContentTypeGrantAsync(userId, tenantId, contentTypeName).ConfigureAwait(false);
        if (grant == null) return;

        grant.RemovePermissions(permissions);
        if (!grant.GetPermissionsAsEnum().Any())
        {
            context.Set<ContentTypePermission>().Remove(grant);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);

        await TrackMutationAsync(
            PermissionOperationType.Revoke,
            userId,
            tenantId,
            permissions.Select(p => p.ToString()),
            contentTypeName).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PermissionType>> GetEffectiveContentTypePermissionsAsync(Guid? userId, Guid? tenantId, string contentTypeName)
    {
        var permissions = new HashSet<PermissionType>();
        AddRange(permissions, await GetEffectiveTenantPermissionsAsync(userId, tenantId).ConfigureAwait(false));
        AddRange(permissions, await GetContentTypePermissionsAsync(null, null, contentTypeName).ConfigureAwait(false));

        if (tenantId.HasValue)
        {
            AddRange(permissions, await GetContentTypePermissionsAsync(null, tenantId, contentTypeName).ConfigureAwait(false));
        }

        if (userId.HasValue)
        {
            AddRange(permissions, await GetContentTypePermissionsAsync(userId, tenantId, contentTypeName).ConfigureAwait(false));
        }

        return permissions;
    }

    public async Task<TPermission> GrantResourcePermissionAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid resourceId, PermissionType[] permissions)
        where TPermission : ResourcePermission<TResource>, new() where TResource : EntityBase
    {
        var grant = await GetResourceGrantAsync<TPermission, TResource>(userId, tenantId, resourceId).ConfigureAwait(false);
        if (grant == null)
        {
            grant = new TPermission
            {
                UserId = userId,
                TenantId = tenantId,
                ResourceId = resourceId,
                ResourceType = typeof(TResource).Name,
                GrantedAt = SystemClock.UtcNow
            };
            context.Set<TPermission>().Add(grant);
        }

        grant.AddPermissions(permissions);
        grant.IsActive = true;
        grant.ExpiresAt = null;

        await context.SaveChangesAsync().ConfigureAwait(false);

        await TrackMutationAsync(
            PermissionOperationType.Grant,
            userId,
            tenantId,
            permissions.Select(p => p.ToString()),
            resourceTypeName: typeof(TResource).Name,
            resourceId: resourceId).ConfigureAwait(false);

        return grant;
    }

    public async Task<bool> HasResourcePermissionAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid resourceId, PermissionType permission)
        where TPermission : ResourcePermission<TResource>, new() where TResource : EntityBase
        => (await GetEffectiveResourcePermissionsAsync<TPermission, TResource>(userId, tenantId, resourceId).ConfigureAwait(false)).Contains(permission);

    public async Task<IEnumerable<PermissionType>> GetResourcePermissionsAsync<TPermission, TResource>(Guid? userId, Guid? tenantId, Guid resourceId)
        where TPermission : ResourcePermission<TResource>, new() where TResource : EntityBase
    {
        if (!userId.HasValue)
        {
            return [];
        }

        var grant = await GetResourceGrantAsync<TPermission, TResource>(userId.Value, tenantId, resourceId).ConfigureAwait(false);
        return grant == null || !grant.IsEffective()
            ? []
            : grant.GetPermissionsAsEnum();
    }

    public async Task BulkGrantResourcePermissionAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid[] resourceIds, PermissionType[] permissions)
        where TPermission : ResourcePermission<TResource>, new() where TResource : EntityBase
    {
        foreach (var resourceId in resourceIds.Where(id => id != Guid.Empty).Distinct())
        {
            await GrantResourcePermissionAsync<TPermission, TResource>(userId, tenantId, resourceId, permissions).ConfigureAwait(false);
        }
    }

    public async Task<Dictionary<Guid, IEnumerable<PermissionType>>> GetBulkResourcePermissionsAsync<TPermission, TResource>(Guid? userId, Guid? tenantId, Guid[] resourceIds)
        where TPermission : ResourcePermission<TResource>, new() where TResource : EntityBase
    {
        var results = new Dictionary<Guid, IEnumerable<PermissionType>>();
        foreach (var resourceId in resourceIds.Distinct())
        {
            results[resourceId] = await GetResourcePermissionsAsync<TPermission, TResource>(userId, tenantId, resourceId).ConfigureAwait(false);
        }

        return results;
    }

    public async Task ShareResourceAsync<TPermission, TResource>(Guid resourceId, Guid targetUserId, Guid? tenantId, PermissionType[] permissions, DateTime? expiresAt = null)
        where TPermission : ResourcePermission<TResource>, new() where TResource : EntityBase
    {
        var grant = await GrantResourcePermissionAsync<TPermission, TResource>(targetUserId, tenantId, resourceId, permissions).ConfigureAwait(false);
        grant.ExpiresAt = expiresAt;
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task RevokeResourceAccessAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid resourceId)
        where TPermission : ResourcePermission<TResource>, new() where TResource : EntityBase
    {
        var grant = await GetResourceGrantAsync<TPermission, TResource>(userId, tenantId, resourceId).ConfigureAwait(false);
        if (grant == null) return;

        context.Set<TPermission>().Remove(grant);
        await context.SaveChangesAsync().ConfigureAwait(false);

        await TrackMutationAsync(
            PermissionOperationType.Revoke,
            userId,
            tenantId,
            [],
            resourceTypeName: typeof(TResource).Name,
            resourceId: resourceId).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PermissionType>> GetEffectiveResourcePermissionsAsync<TPermission, TResource>(Guid? userId, Guid? tenantId, Guid resourceId, string? contentTypeName = null)
        where TPermission : ResourcePermission<TResource>, new() where TResource : EntityBase
    {
        var permissions = new HashSet<PermissionType>();
        if (!string.IsNullOrWhiteSpace(contentTypeName))
        {
            AddRange(permissions, await GetEffectiveContentTypePermissionsAsync(userId, tenantId, contentTypeName).ConfigureAwait(false));
        }
        else
        {
            AddRange(permissions, await GetEffectiveTenantPermissionsAsync(userId, tenantId).ConfigureAwait(false));
        }

        AddRange(permissions, await GetResourcePermissionsAsync<TPermission, TResource>(userId, tenantId, resourceId).ConfigureAwait(false));
        return permissions;
    }

    public async Task<IEnumerable<PermissionType>> ResolveEffectivePermissionsAsync(Guid? userId, Guid? tenantId, string? contentTypeName = null, Guid? resourceId = null, string? resourceTypeName = null)
    {
        var permissions = new HashSet<PermissionType>();
        if (!string.IsNullOrWhiteSpace(contentTypeName))
        {
            AddRange(permissions, await GetEffectiveContentTypePermissionsAsync(userId, tenantId, contentTypeName).ConfigureAwait(false));
        }
        else
        {
            AddRange(permissions, await GetEffectiveTenantPermissionsAsync(userId, tenantId).ConfigureAwait(false));
        }

        if (userId.HasValue && resourceId.HasValue)
        {
            var query = context.Set<GenericResourcePermission>()
                .Where(p => p.UserId == userId && p.TenantId == tenantId && p.ResourceId == resourceId && p.IsActive);

            if (!string.IsNullOrWhiteSpace(resourceTypeName))
            {
                query = query.Where(p => p.ResourceType == resourceTypeName);
            }

            var resourceGrant = await query.FirstOrDefaultAsync().ConfigureAwait(false);
            if (resourceGrant is { } grant && grant.IsEffective())
            {
                AddRange(permissions, grant.GetPermissionsAsEnum());
            }
        }

        return permissions;
    }

    public async Task<bool> HasPermissionAsync(Guid? userId, Guid? tenantId, PermissionType permission, string? contentTypeName = null, Guid? resourceId = null, string? resourceTypeName = null)
        => (await ResolveEffectivePermissionsAsync(userId, tenantId, contentTypeName, resourceId, resourceTypeName).ConfigureAwait(false)).Contains(permission);

    public async Task<string> GetPermissionSourceAsync(Guid? userId, Guid? tenantId, PermissionType permission, string? contentTypeName = null, Guid? resourceId = null, string? resourceTypeName = null)
    {
        if (userId.HasValue && resourceId.HasValue)
        {
            var resourcePermissions = await GetResourcePermissionsAsync<GenericResourcePermission, EntityBase>(userId, tenantId, resourceId.Value).ConfigureAwait(false);
            if (resourcePermissions.Contains(permission)) return "Resource";
        }

        if (!string.IsNullOrWhiteSpace(contentTypeName))
        {
            var contentPermissions = await GetContentTypePermissionsAsync(userId, tenantId, contentTypeName).ConfigureAwait(false);
            if (contentPermissions.Contains(permission)) return "ContentType";
        }

        var tenantPermissions = await GetTenantPermissionsAsync(userId, tenantId).ConfigureAwait(false);
        if (tenantPermissions.Contains(permission)) return "Tenant";

        if (tenantId.HasValue)
        {
            var tenantDefaults = await GetTenantDefaultPermissionsAsync(tenantId.Value).ConfigureAwait(false);
            if (tenantDefaults.Contains(permission)) return "TenantDefault";
        }

        var globalDefaults = await GetGlobalDefaultPermissionsAsync().ConfigureAwait(false);
        return globalDefaults.Contains(permission) ? "GlobalDefault" : "None";
    }

    public async Task<IEnumerable<Guid>> GetUsersWithPermissionAsync(Guid tenantId, PermissionType permission)
    {
        var permissionName = permission.ToString();
        var grants = await context.Set<TenantPermission>()
            .Where(p => p.TenantId == tenantId && p.UserId != null && p.IsActive)
            .ToListAsync()
            .ConfigureAwait(false);

        return grants
            .Where(p => !p.IsExpired() && p.HasPermission(permissionName))
            .Select(p => p.UserId!.Value)
            .Distinct()
            .ToArray();
    }

    public async Task<IEnumerable<Guid>> GetResourcesWithPermissionAsync(Guid userId, Guid? tenantId, PermissionType permission, string? resourceTypeName = null)
    {
        var query = context.Set<GenericResourcePermission>()
            .Where(p => p.UserId == userId && p.TenantId == tenantId && p.IsActive);

        if (!string.IsNullOrWhiteSpace(resourceTypeName))
        {
            query = query.Where(p => p.ResourceType == resourceTypeName);
        }

        var grants = await query.ToListAsync().ConfigureAwait(false);
        return grants
            .Where(p => p.IsEffective() && p.HasPermission(permission))
            .Select(p => p.ResourceId)
            .Distinct()
            .ToArray();
    }

    public async Task<Dictionary<Guid, Dictionary<PermissionType, bool>>> BulkCheckPermissionsAsync(Guid[] userIds, Guid? tenantId, PermissionType[] permissions)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        ArgumentNullException.ThrowIfNull(permissions);

        var distinctUserIds = userIds.Distinct().ToArray();
        var distinctPermissions = permissions.Distinct().ToArray();
        var results = distinctUserIds.ToDictionary(
            userId => userId,
            _ => new Dictionary<PermissionType, bool>());

        if (distinctUserIds.Length == 0 || distinctPermissions.Length == 0)
        {
            return results;
        }

        // Load global defaults, tenant defaults, and all requested users' direct grants
        // in one database round-trip instead of resolving every user/permission pair
        // independently (which previously produced O(users * permissions) queries).
        var grants = await context.Set<TenantPermission>()
            .AsNoTracking()
            .Where(grant =>
                grant.UserId == null && (grant.TenantId == null || grant.TenantId == tenantId) ||
                grant.UserId != null && grant.TenantId == tenantId && distinctUserIds.Contains(grant.UserId.Value))
            .ToListAsync()
            .ConfigureAwait(false);

        var activeGrants = grants.Where(grant => grant.IsActive && !grant.IsExpired()).ToArray();
        var defaultPermissions = new HashSet<PermissionType>(
            activeGrants
                .Where(grant => grant.UserId == null)
                .SelectMany(grant => ToPermissionTypes(grant.Permissions)));

        foreach (var userId in distinctUserIds)
        {
            var effectivePermissions = new HashSet<PermissionType>(defaultPermissions);
            foreach (var grant in activeGrants.Where(grant => grant.UserId == userId))
            {
                effectivePermissions.UnionWith(ToPermissionTypes(grant.Permissions));
            }

            foreach (var permission in distinctPermissions)
            {
                results[userId][permission] = effectivePermissions.Contains(permission);
            }
        }

        return results;
    }

    public async Task<IReadOnlyList<BulkPermissionCheckResult>> BulkCheckPermissionsAsync(
        IReadOnlyCollection<BulkPermissionCheckRequest> requests,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);

        var results = new List<BulkPermissionCheckResult>(requests.Count);
        await foreach (var result in StreamBulkCheckPermissionsAsync(
                           ToAsyncEnumerable(requests, cancellationToken),
                           MaximumBulkPermissionCheckBatchSize,
                           cancellationToken).ConfigureAwait(false))
        {
            results.Add(result);
        }

        return results;
    }

    public async IAsyncEnumerable<BulkPermissionCheckResult> StreamBulkCheckPermissionsAsync(
        IAsyncEnumerable<BulkPermissionCheckRequest> requests,
        int batchSize = 128,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);
        if (batchSize is < 1 or > MaximumBulkPermissionCheckBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                $"Batch size must be between 1 and {MaximumBulkPermissionCheckBatchSize}.");
        }

        var batch = new List<BulkPermissionCheckRequest>(batchSize);
        await foreach (var request in requests.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            ValidateBulkPermissionCheckRequest(request);
            batch.Add(request);
            if (batch.Count < batchSize) continue;

            var results = await EvaluateBulkPermissionCheckBatchAsync(batch, cancellationToken).ConfigureAwait(false);
            foreach (var result in results)
            {
                yield return result;
            }

            batch.Clear();
        }

        if (batch.Count == 0) yield break;

        var finalResults = await EvaluateBulkPermissionCheckBatchAsync(batch, cancellationToken).ConfigureAwait(false);
        foreach (var result in finalResults)
        {
            yield return result;
        }
    }

    private async Task<IReadOnlyList<BulkPermissionCheckResult>> EvaluateBulkPermissionCheckBatchAsync(
        IReadOnlyList<BulkPermissionCheckRequest> requests,
        CancellationToken cancellationToken)
    {
        var userIds = requests.Select(request => request.UserId).Distinct().ToArray();
        var tenantIds = requests.Where(request => request.TenantId.HasValue)
            .Select(request => request.TenantId!.Value)
            .Distinct()
            .ToArray();

        var tenantGrants = await context.Set<TenantPermission>()
            .AsNoTracking()
            .Where(grant =>
                (grant.UserId == null || userIds.Contains(grant.UserId.Value)) &&
                (grant.TenantId == null || tenantIds.Contains(grant.TenantId.Value)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var contentTypeNames = requests
            .Where(request => !string.IsNullOrWhiteSpace(request.ContentTypeName))
            .Select(request => request.ContentTypeName!)
            .Distinct()
            .ToArray();
        var contentTypeGrants = contentTypeNames.Length == 0
            ? []
            : await context.Set<ContentTypePermission>()
                .AsNoTracking()
                .Where(grant =>
                    contentTypeNames.Contains(grant.ContentTypeName) &&
                    (grant.UserId == null || userIds.Contains(grant.UserId.Value)) &&
                    (grant.TenantId == null || tenantIds.Contains(grant.TenantId.Value)))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var resourceRequests = requests.Where(request => request.ResourceId.HasValue).ToArray();
        var resourceIds = resourceRequests.Select(request => request.ResourceId!.Value).Distinct().ToArray();
        var resourceGrants = resourceRequests.Length == 0
            ? []
            : await context.Set<GenericResourcePermission>()
                .AsNoTracking()
                .Where(grant =>
                    resourceIds.Contains(grant.ResourceId) &&
                    grant.UserId.HasValue && userIds.Contains(grant.UserId.Value) &&
                    (grant.TenantId == null || tenantIds.Contains(grant.TenantId.Value)))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var result = new List<BulkPermissionCheckResult>(requests.Count);
        var batchCache = new Dictionary<BulkPermissionCheckRequest, bool>();
        foreach (var request in requests)
        {
            if (batchCache.TryGetValue(request, out var cachedDecision))
            {
                result.Add(new BulkPermissionCheckResult(request, cachedDecision));
                continue;
            }

            var allowed = new HashSet<PermissionType>();
            var denied = new HashSet<PermissionType>();

            foreach (var grant in tenantGrants)
            {
                if (!grant.IsActive || grant.IsExpired() || !TenantGrantApplies(grant, request)) continue;
                allowed.UnionWith(ToPermissionTypes(grant.Permissions));
                denied.UnionWith(ToPermissionTypes(grant.DenyPermissions));
            }

            if (!string.IsNullOrWhiteSpace(request.ContentTypeName))
            {
                foreach (var grant in contentTypeGrants)
                {
                    if (!grant.IsEffective() ||
                        !string.Equals(grant.ContentTypeName, request.ContentTypeName, StringComparison.Ordinal) ||
                        !ContentTypeGrantApplies(grant, request)) continue;

                    allowed.UnionWith(grant.GetPermissionsAsEnum());
                }
            }

            if (request.ResourceId.HasValue)
            {
                foreach (var grant in resourceGrants)
                {
                    if (!grant.IsEffective() ||
                        grant.UserId != request.UserId ||
                        grant.TenantId != request.TenantId ||
                        grant.ResourceId != request.ResourceId.Value ||
                        !string.Equals(grant.ResourceType, request.ResourceTypeName, StringComparison.Ordinal)) continue;

                    allowed.UnionWith(grant.GetPermissionsAsEnum());
                }
            }

            var isGranted = allowed.Contains(request.Permission) && !denied.Contains(request.Permission);
            batchCache.Add(request, isGranted);
            result.Add(new BulkPermissionCheckResult(request, isGranted));
        }

        return result;
    }

    private static bool TenantGrantApplies(TenantPermission grant, BulkPermissionCheckRequest request)
        => (grant.UserId == null && grant.TenantId == null) ||
           (grant.TenantId == request.TenantId && (grant.UserId == null || grant.UserId == request.UserId));

    private static bool ContentTypeGrantApplies(ContentTypePermission grant, BulkPermissionCheckRequest request)
        => (grant.UserId == null && grant.TenantId == null) ||
           (grant.TenantId == request.TenantId && (grant.UserId == null || grant.UserId == request.UserId));

    private static void ValidateBulkPermissionCheckRequest(BulkPermissionCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ResourceId.HasValue && string.IsNullOrWhiteSpace(request.ResourceTypeName))
        {
            throw new ArgumentException("ResourceTypeName is required when ResourceId is provided.", nameof(request));
        }

        if (!request.ResourceId.HasValue && !string.IsNullOrWhiteSpace(request.ResourceTypeName))
        {
            throw new ArgumentException("ResourceTypeName cannot be provided without ResourceId.", nameof(request));
        }
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        IEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    public async Task CleanupExpiredPermissionsAsync()
    {
        var tenantPermissions = await context.Set<TenantPermission>()
            .Where(p => p.IsActive && p.ExpiresAt != null && p.ExpiresAt <= SystemClock.UtcNow)
            .ToListAsync()
            .ConfigureAwait(false);
        foreach (var grant in tenantPermissions)
        {
            grant.IsActive = false;
        }

        var contentPermissions = await context.Set<ContentTypePermission>()
            .Where(p => p.IsActive && p.ExpiresAt != null && p.ExpiresAt <= SystemClock.UtcNow)
            .ToListAsync()
            .ConfigureAwait(false);
        foreach (var grant in contentPermissions)
        {
            grant.Expire();
        }

        var resourcePermissions = await context.Set<GenericResourcePermission>()
            .Where(p => p.IsActive && p.ExpiresAt != null && p.ExpiresAt <= SystemClock.UtcNow)
            .ToListAsync()
            .ConfigureAwait(false);
        foreach (var grant in resourcePermissions)
        {
            grant.Expire();
        }

        await context.SaveChangesAsync().ConfigureAwait(false);

        // Expiring grants changes effective permissions: bump every affected tenant's
        // security version so stale caches are discarded, then audit the cleanup.
        var affectedTenants = tenantPermissions
            .Select(p => p.TenantId)
            .Concat(contentPermissions.Select(p => (Guid?)p.TenantId))
            .Concat(resourcePermissions.Select(p => (Guid?)p.TenantId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        foreach (var tenantId in affectedTenants)
        {
            await TrackMutationAsync(
                PermissionOperationType.Update,
                userId: null,
                tenantId: tenantId,
                permissionNames: [],
                reason: "Expired permissions cleanup").ConfigureAwait(false);
        }
    }

    private async Task SetTenantGrantAsync(Guid? userId, Guid? tenantId, PermissionType[] permissions, string reason)
    {
        var grant = await GetTenantGrantAsync(userId, tenantId).ConfigureAwait(false);
        if (grant == null)
        {
            grant = new TenantPermission
            {
                UserId = userId,
                TenantId = tenantId,
                GrantedAt = SystemClock.UtcNow,
                Reason = reason
            };
            context.Set<TenantPermission>().Add(grant);
        }

        grant.Permissions = ToPermissionNames(permissions);
        grant.IsActive = true;
        grant.ExpiresAt = null;
        await context.SaveChangesAsync().ConfigureAwait(false);

        await TrackMutationAsync(
            PermissionOperationType.Update,
            userId,
            tenantId,
            permissions.Select(p => p.ToString()),
            reason: reason).ConfigureAwait(false);
    }

    private Task<TenantPermission?> GetTenantGrantAsync(Guid? userId, Guid? tenantId)
        => context.Set<TenantPermission>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId);

    private Task<ContentTypePermission?> GetContentTypeGrantAsync(Guid? userId, Guid? tenantId, string contentTypeName)
        => context.Set<ContentTypePermission>()
            .FirstOrDefaultAsync(p =>
                p.UserId == userId &&
                p.TenantId == tenantId &&
                p.ContentTypeName == contentTypeName);

    private Task<TPermission?> GetResourceGrantAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid resourceId)
        where TPermission : ResourcePermission<TResource>, new() where TResource : EntityBase
        => context.Set<TPermission>()
            .FirstOrDefaultAsync(p =>
                p.UserId == userId &&
                p.TenantId == tenantId &&
                p.ResourceId == resourceId);

    /// <summary>
    ///     SECURITY: records a permission mutation — bumps the tenant security version so
    ///     cached permission sets are invalidated, and writes a permission audit entry.
    ///     Failures propagate: a mutation whose invalidation failed must not be reported as
    ///     silently successful (stale caches would retain revoked privileges).
    /// </summary>
    private async Task TrackMutationAsync(
        PermissionOperationType operation,
        Guid? userId,
        Guid? tenantId,
        IEnumerable<string> permissionNames,
        string? contentTypeName = null,
        string? resourceTypeName = null,
        Guid? resourceId = null,
        string? reason = null)
    {
        var permissionList = permissionNames as string[] ?? permissionNames.ToArray();
        var performedBy = actorContextAccessor?.ActorContext.SubjectIdAsGuid ?? Guid.Empty;

        if (securityVersionStore is not null)
        {
            var tenantKey = tenantId?.ToString() ?? Guid.Empty.ToString();
            await securityVersionStore.IncrementVersionAsync(tenantKey).ConfigureAwait(false);
        }

        if (auditService is not null)
        {
            await auditService.LogPermissionChangeAsync(
                operation,
                userId,
                performedBy,
                tenantId,
                permissionType: permissionList.Length > 0 ? string.Join(",", permissionList) : contentTypeName ?? resourceTypeName,
                resourceId: resourceId,
                resourceType: resourceTypeName ?? contentTypeName,
                reason: reason ?? $"{operation} via legacy permission facade").ConfigureAwait(false);
        }
    }

    private static string[] ToPermissionNames(IEnumerable<PermissionType> permissions)
        => permissions.Distinct().Select(p => p.ToString()).ToArray();

    private static IEnumerable<PermissionType> ToPermissionTypes(IEnumerable<string> permissions)
    {
        foreach (var permission in permissions)
        {
            if (Enum.TryParse<PermissionType>(permission, ignoreCase: true, out var parsed))
            {
                yield return parsed;
            }
            else if (int.TryParse(permission, out var numeric) && Enum.IsDefined(typeof(PermissionType), numeric))
            {
                yield return (PermissionType)numeric;
            }
        }
    }

    private static void AddRange(HashSet<PermissionType> target, IEnumerable<PermissionType> permissions)
    {
        foreach (var permission in permissions)
        {
            target.Add(permission);
        }
    }
}
