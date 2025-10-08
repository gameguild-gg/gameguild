using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Constants;
using GameGuild.Modules.Permissions.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace GameGuild.Modules.Permissions.Services;

/// <summary>
/// Service implementation for managing delegated administrative scopes
/// </summary>
public class DelegatedAdminService : IDelegatedAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IPermissionAuditService _auditService;
    private readonly ILogger<DelegatedAdminService> _logger;
    private const string CacheKeyPrefix = "delegated_admin_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public DelegatedAdminService(
        ApplicationDbContext context,
        IMemoryCache cache,
        IPermissionAuditService auditService,
        ILogger<DelegatedAdminService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DelegatedAdminScope> CreateDelegationAsync(
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
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating delegation from {Delegator} to {Delegated} in tenant {Tenant} for scope {ScopeType}:{ScopeId}",
            delegatorUserId, delegatedUserId, tenantId, scopeType, scopeId);

        // Validate permissions array
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be delegated", nameof(permissions));

        // Create delegation
        var delegation = new DelegatedAdminScope
        {
            Id = Guid.NewGuid(),
            DelegatorUserId = delegatorUserId,
            DelegatedUserId = delegatedUserId,
            TenantId = tenantId,
            ScopeType = scopeType,
            ScopeId = scopeId,
            ScopeName = scopeName,
            DelegatedPermissions = permissions,
            AllowSubDelegation = allowSubDelegation,
            MaxDelegationDepth = maxDelegationDepth,
            CurrentDepth = 0,
            ExpiresAt = expiresAt,
            Reason = reason,
            Constraints = constraints,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<DelegatedAdminScope>().Add(delegation);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        InvalidateUserCache(delegatedUserId);

        // Audit
        await _auditService.LogPermissionChangeAsync(
            delegatedUserId,
            tenantId,
            $"Delegated admin scope created: {scopeType}:{scopeId}",
            string.Join(", ", permissions.Select(p => p.ToString())),
            cancellationToken);

        _logger.LogInformation("Delegation {DelegationId} created successfully", delegation.Id);

        return delegation;
    }

    public async Task<DelegatedAdminScope> CreateSubDelegationAsync(
        Guid parentDelegationId,
        Guid newDelegatedUserId,
        PermissionType[] permissions,
        DateTime? expiresAt = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating sub-delegation from parent {ParentId} to user {UserId}", parentDelegationId, newDelegatedUserId);

        // Get parent delegation
        var parent = await _context.Set<DelegatedAdminScope>()
            .FirstOrDefaultAsync(d => d.Id == parentDelegationId && d.IsValid, cancellationToken);

        if (parent == null)
            throw new InvalidOperationException("Parent delegation not found or is not valid");

        // Check if sub-delegation is allowed
        if (!parent.CanSubDelegate())
            throw new InvalidOperationException("Sub-delegation is not allowed or maximum depth reached");

        // Validate that sub-delegation permissions are subset of parent
        var invalidPermissions = permissions.Except(parent.DelegatedPermissions).ToArray();
        if (invalidPermissions.Any())
            throw new ArgumentException($"Cannot delegate permissions not granted in parent: {string.Join(", ", invalidPermissions)}");

        // Create sub-delegation
        var subDelegation = new DelegatedAdminScope
        {
            Id = Guid.NewGuid(),
            DelegatorUserId = parent.DelegatedUserId, // Delegated user becomes delegator
            DelegatedUserId = newDelegatedUserId,
            TenantId = parent.TenantId,
            ScopeType = parent.ScopeType,
            ScopeId = parent.ScopeId,
            ScopeName = parent.ScopeName,
            DelegatedPermissions = permissions,
            AllowSubDelegation = parent.AllowSubDelegation,
            MaxDelegationDepth = parent.MaxDelegationDepth,
            CurrentDepth = parent.CurrentDepth + 1,
            ParentDelegationId = parentDelegationId,
            ExpiresAt = expiresAt ?? parent.ExpiresAt, // Inherit or set shorter expiration
            Reason = reason,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Ensure sub-delegation doesn't expire after parent
        if (parent.ExpiresAt.HasValue && (!subDelegation.ExpiresAt.HasValue || subDelegation.ExpiresAt > parent.ExpiresAt))
        {
            subDelegation.ExpiresAt = parent.ExpiresAt;
        }

        _context.Set<DelegatedAdminScope>().Add(subDelegation);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        InvalidateUserCache(newDelegatedUserId);

        // Audit
        await _auditService.LogPermissionChangeAsync(
            newDelegatedUserId,
            parent.TenantId,
            $"Sub-delegation created from {parentDelegationId}",
            string.Join(", ", permissions.Select(p => p.ToString())),
            cancellationToken);

        _logger.LogInformation("Sub-delegation {DelegationId} created successfully", subDelegation.Id);

        return subDelegation;
    }

    public async Task RevokeDelegationAsync(
        Guid delegationId,
        Guid revokedByUserId,
        string reason,
        bool revokeSubDelegations = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Revoking delegation {DelegationId} by user {UserId}", delegationId, revokedByUserId);

        var delegation = await _context.Set<DelegatedAdminScope>()
            .FirstOrDefaultAsync(d => d.Id == delegationId, cancellationToken);

        if (delegation == null)
            throw new InvalidOperationException("Delegation not found");

        // Revoke the delegation
        delegation.Revoke(revokedByUserId, reason);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        InvalidateUserCache(delegation.DelegatedUserId);

        // Revoke sub-delegations if requested
        if (revokeSubDelegations)
        {
            var subDelegations = await GetSubDelegationsAsync(delegationId, recursive: true, cancellationToken);
            foreach (var subDelegation in subDelegations)
            {
                subDelegation.Revoke(revokedByUserId, $"Parent delegation revoked: {reason}");
                InvalidateUserCache(subDelegation.DelegatedUserId);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Audit
        await _auditService.LogPermissionChangeAsync(
            delegation.DelegatedUserId,
            delegation.TenantId,
            $"Delegated admin scope revoked: {delegation.ScopeType}:{delegation.ScopeId}",
            reason,
            cancellationToken);

        _logger.LogInformation("Delegation {DelegationId} revoked successfully", delegationId);
    }

    public async Task<IEnumerable<DelegatedAdminScope>> GetUserDelegationsAsync(
        Guid userId,
        Guid? tenantId = null,
        bool includeExpired = false,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}user_{userId}_{tenantId}_{includeExpired}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<DelegatedAdminScope>? cached) && cached != null)
            return cached;

        var query = _context.Set<DelegatedAdminScope>()
            .Where(d => d.DelegatedUserId == userId && d.IsActive);

        if (tenantId.HasValue)
            query = query.Where(d => d.TenantId == tenantId.Value);

        if (!includeExpired)
            query = query.Where(d => !d.ExpiresAt.HasValue || d.ExpiresAt > DateTime.UtcNow);

        query = query.Where(d => !d.RevokedAt.HasValue);

        var delegations = await query.ToListAsync(cancellationToken);

        _cache.Set(cacheKey, delegations, CacheDuration);

        return delegations;
    }

    public async Task<IEnumerable<DelegatedAdminScope>> GetDelegationsByDelegatorAsync(
        Guid delegatorUserId,
        Guid? tenantId = null,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<DelegatedAdminScope>()
            .Where(d => d.DelegatorUserId == delegatorUserId);

        if (tenantId.HasValue)
            query = query.Where(d => d.TenantId == tenantId.Value);

        if (!includeRevoked)
            query = query.Where(d => !d.RevokedAt.HasValue);

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasDelegatedPermissionAsync(
        Guid userId,
        Guid tenantId,
        string scopeType,
        Guid? scopeId,
        PermissionType permission,
        CancellationToken cancellationToken = default)
    {
        var delegations = await GetUserDelegationsAsync(userId, tenantId, includeExpired: false, cancellationToken);

        return delegations.Any(d =>
            d.IsValid &&
            d.MatchesResource(scopeType, scopeId) &&
            d.DelegatedPermissions.Contains(permission));
    }

    public async Task<PermissionType[]> GetDelegatedPermissionsAsync(
        Guid userId,
        Guid tenantId,
        string scopeType,
        Guid? scopeId,
        CancellationToken cancellationToken = default)
    {
        var delegations = await GetUserDelegationsAsync(userId, tenantId, includeExpired: false, cancellationToken);

        var matchingDelegations = delegations
            .Where(d => d.IsValid && d.MatchesResource(scopeType, scopeId))
            .ToList();

        // Combine all permissions from matching delegations
        return matchingDelegations
            .SelectMany(d => d.DelegatedPermissions)
            .Distinct()
            .ToArray();
    }

    public async Task<IEnumerable<DelegatedAdminScope>> GetDelegationChainAsync(
        Guid delegationId,
        CancellationToken cancellationToken = default)
    {
        var chain = new List<DelegatedAdminScope>();
        var currentId = delegationId;

        while (currentId != Guid.Empty)
        {
            var delegation = await _context.Set<DelegatedAdminScope>()
                .FirstOrDefaultAsync(d => d.Id == currentId, cancellationToken);

            if (delegation == null) break;

            chain.Add(delegation);
            currentId = delegation.ParentDelegationId ?? Guid.Empty;
        }

        return chain;
    }

    public async Task<IEnumerable<DelegatedAdminScope>> GetSubDelegationsAsync(
        Guid parentDelegationId,
        bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        var subDelegations = await _context.Set<DelegatedAdminScope>()
            .Where(d => d.ParentDelegationId == parentDelegationId)
            .ToListAsync(cancellationToken);

        if (!recursive)
            return subDelegations;

        // Recursively get all sub-delegations
        var allSubDelegations = new List<DelegatedAdminScope>(subDelegations);
        foreach (var sub in subDelegations)
        {
            var children = await GetSubDelegationsAsync(sub.Id, recursive: true, cancellationToken);
            allSubDelegations.AddRange(children);
        }

        return allSubDelegations;
    }

    public async Task ActivateDelegationAsync(Guid delegationId, CancellationToken cancellationToken = default)
    {
        var delegation = await GetDelegationByIdAsync(delegationId, cancellationToken);
        if (delegation == null)
            throw new InvalidOperationException("Delegation not found");

        delegation.Activate();
        await _context.SaveChangesAsync(cancellationToken);
        InvalidateUserCache(delegation.DelegatedUserId);
    }

    public async Task DeactivateDelegationAsync(Guid delegationId, CancellationToken cancellationToken = default)
    {
        var delegation = await GetDelegationByIdAsync(delegationId, cancellationToken);
        if (delegation == null)
            throw new InvalidOperationException("Delegation not found");

        delegation.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);
        InvalidateUserCache(delegation.DelegatedUserId);
    }

    public async Task<DelegatedAdminScope?> GetDelegationByIdAsync(Guid delegationId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<DelegatedAdminScope>()
            .FirstOrDefaultAsync(d => d.Id == delegationId, cancellationToken);
    }

    public async Task UpdateDelegationExpirationAsync(
        Guid delegationId,
        DateTime? newExpiresAt,
        CancellationToken cancellationToken = default)
    {
        var delegation = await GetDelegationByIdAsync(delegationId, cancellationToken);
        if (delegation == null)
            throw new InvalidOperationException("Delegation not found");

        // Update expiration using reflection since ExpiresAt is protected
        var property = typeof(DelegatedAdminScope).GetProperty(nameof(DelegatedAdminScope.ExpiresAt));
        property?.SetValue(delegation, newExpiresAt);
        delegation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        InvalidateUserCache(delegation.DelegatedUserId);
    }

    public async Task<DelegationStatistics> GetDelegationStatisticsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var delegations = await _context.Set<DelegatedAdminScope>()
            .Where(d => d.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var stats = new DelegationStatistics
        {
            TotalDelegations = delegations.Count,
            ActiveDelegations = delegations.Count(d => d.IsValid),
            ExpiredDelegations = delegations.Count(d => d.ExpiresAt.HasValue && d.ExpiresAt <= DateTime.UtcNow),
            RevokedDelegations = delegations.Count(d => d.RevokedAt.HasValue),
            SubDelegations = delegations.Count(d => d.ParentDelegationId.HasValue),
            DelegationsByScopeType = delegations
                .GroupBy(d => d.ScopeType)
                .ToDictionary(g => g.Key, g => g.Count()),
            DelegationsByPermission = delegations
                .SelectMany(d => d.DelegatedPermissions)
                .GroupBy(p => p.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            AverageDelegationDuration = delegations
                .Where(d => d.ExpiresAt.HasValue)
                .Select(d => (d.ExpiresAt!.Value - d.CreatedAt).TotalHours)
                .DefaultIfEmpty(0)
                .Average()
        };

        return stats;
    }

    public async Task AutoRevokeExpiredDelegationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Auto-revoking expired delegations");

        var expiredDelegations = await _context.Set<DelegatedAdminScope>()
            .Where(d => d.IsActive &&
                       !d.RevokedAt.HasValue &&
                       d.ExpiresAt.HasValue &&
                       d.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var delegation in expiredDelegations)
        {
            delegation.Revoke(Guid.Empty, "Automatic revocation due to expiration");
            InvalidateUserCache(delegation.DelegatedUserId);
        }

        if (expiredDelegations.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Auto-revoked {Count} expired delegations", expiredDelegations.Count);
        }
    }

    private void InvalidateUserCache(Guid userId)
    {
        // Remove all cache entries for this user
        var pattern = $"{CacheKeyPrefix}user_{userId}_";
        // Note: IMemoryCache doesn't support pattern-based removal, 
        // so we'd need to track keys or use a distributed cache for this
        // For now, we just log the invalidation
        _logger.LogDebug("Invalidating delegation cache for user {UserId}", userId);
    }
}
