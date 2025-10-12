using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Entities;

namespace GameGuild.Modules.Permissions.Services;

/// <summary>
/// Service for managing permission delegations
/// </summary>
public class PermissionDelegationService : IPermissionDelegationService
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionDelegationService> _logger;

    public PermissionDelegationService(
        ApplicationDbContext context,
        IPermissionService permissionService,
        ILogger<PermissionDelegationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PermissionDelegation> CreateDelegationAsync(
        Guid delegatorUserId,
        Guid delegateUserId,
        Guid? tenantId,
        Guid? resourceId,
        PermissionType[] permissions,
        DateTime? expiresAt = null,
        bool canSubDelegate = false,
        string? reason = null,
        int? usageLimit = null)
    {
        // Verify delegator has the permissions they want to delegate
        var canDelegate = await CanDelegatePermissionsAsync(delegatorUserId, tenantId, resourceId, permissions);
        if (!canDelegate)
        {
            throw new UnauthorizedAccessException("User does not have permissions to delegate");
        }

        var delegation = new PermissionDelegation
        {
            DelegatorUserId = delegatorUserId,
            DelegateUserId = delegateUserId,
            TenantId = tenantId,
            ResourceId = resourceId,
            DelegatedPermissions = permissions,
            ExpiresAt = expiresAt,
            CanSubDelegate = canSubDelegate,
            Reason = reason,
            UsageLimit = usageLimit,
            IsActive = true
        };

        _context.PermissionDelegations.Add(delegation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created permission delegation from User:{DelegatorId} to User:{DelegateId} for permissions:{Permissions}",
            delegatorUserId, delegateUserId, string.Join(", ", permissions));

        return delegation;
    }

    public async Task RevokeDelegationAsync(Guid delegationId, Guid revokingUserId)
    {
        var delegation = await _context.PermissionDelegations
            .FirstOrDefaultAsync(d => d.Id == delegationId && d.IsActive);

        if (delegation == null)
        {
            throw new ArgumentException($"Active delegation with ID {delegationId} not found");
        }

        // Only the delegator or an admin can revoke a delegation
        if (delegation.DelegatorUserId != revokingUserId)
        {
            // Check if revoking user has admin permissions
            var hasAdminPermission = await _permissionService.HasTenantPermissionAsync(
                revokingUserId, delegation.TenantId, PermissionType.Admin);

            if (!hasAdminPermission)
            {
                throw new UnauthorizedAccessException("Only the delegator or an admin can revoke this delegation");
            }
        }

        delegation.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked permission delegation {DelegationId} by User:{RevokingUserId}",
            delegationId, revokingUserId);
    }

    public async Task<bool> HasDelegatedPermissionAsync(
        Guid userId,
        Guid? tenantId,
        Guid? resourceId,
        PermissionType permission)
    {
        var activeDelegations = await _context.PermissionDelegations
            .Where(d => d.DelegateUserId == userId
                && d.TenantId == tenantId
                && d.ResourceId == resourceId
                && d.IsActive
                && d.StartsAt <= DateTime.UtcNow
                && (d.ExpiresAt == null || d.ExpiresAt > DateTime.UtcNow)
                && (d.UsageLimit == null || d.UsageCount < d.UsageLimit))
            .ToListAsync();

        return activeDelegations.Any(d => d.DelegatedPermissions.Contains(permission));
    }

    public async Task<IEnumerable<PermissionDelegation>> GetUserDelegationsAsync(Guid userId)
    {
        return await _context.PermissionDelegations
            .Where(d => d.DelegateUserId == userId && d.IsValidNow)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PermissionDelegation>> GetCreatedDelegationsAsync(Guid delegatorUserId)
    {
        return await _context.PermissionDelegations
            .Where(d => d.DelegatorUserId == delegatorUserId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PermissionDelegation>> GetTenantDelegationsAsync(Guid tenantId)
    {
        return await _context.PermissionDelegations
            .Where(d => d.TenantId == tenantId && d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PermissionDelegation>> GetResourceDelegationsAsync(Guid resourceId)
    {
        return await _context.PermissionDelegations
            .Where(d => d.ResourceId == resourceId && d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task RecordDelegationUsageAsync(Guid delegationId)
    {
        var delegation = await _context.PermissionDelegations
            .FirstOrDefaultAsync(d => d.Id == delegationId && d.IsActive);

        if (delegation != null)
        {
            delegation.RecordUsage();
            await _context.SaveChangesAsync();

            _logger.LogDebug("Recorded usage for permission delegation {DelegationId}, Count:{UsageCount}",
                delegationId, delegation.UsageCount);
        }
    }

    public async Task CleanupExpiredDelegationsAsync()
    {
        var expiredDelegations = await _context.PermissionDelegations
            .Where(d => d.IsActive && d.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var delegation in expiredDelegations)
        {
            delegation.IsActive = false;
        }

        if (expiredDelegations.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired permission delegations", expiredDelegations.Count);
        }
    }

    public async Task<bool> CanDelegatePermissionsAsync(
        Guid delegatorUserId,
        Guid? tenantId,
        Guid? resourceId,
        PermissionType[] permissions)
    {
        // For simplicity, check if user has all the permissions they want to delegate
        // In a more complex system, you might have different rules for delegation

        foreach (var permission in permissions)
        {
            bool hasPermission;

            if (resourceId.HasValue)
            {
                // For resource permissions, this would need to be implemented based on your resource permission logic
                hasPermission = false; // Placeholder
            }
            else
            {
                hasPermission = await _permissionService.HasTenantPermissionAsync(delegatorUserId, tenantId, permission);
            }

            if (!hasPermission)
            {
                return false;
            }
        }

        return true;
    }
}