using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Service for bulk permission operations and tenant membership.
///     Extracted from PermissionService to follow SRP and improve testability.
/// </summary>
/// <remarks>
///     <b>Design:</b> This service delegates to IPermissionGrantService and IPermissionQueryService
///     for individual operations, providing a higher-level API for bulk operations.
/// </remarks>
public sealed class PermissionBulkService(
    IPermissionGrantService grantService,
    IPermissionQueryService queryService,
    ILogger<PermissionBulkService> logger
) : IPermissionBulkService
{
    public async Task<List<TenantPermission>> BulkGrantTenantPermissionAsync(
        Guid[] userIds,
        Guid tenantId,
        string[] permissions,
        Guid? grantedBy = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Bulk granting permissions {Permissions} to {UserCount} users in tenant {TenantId}",
            string.Join(", ", permissions),
            userIds.Length,
            tenantId);

        var results = new List<TenantPermission>();

        foreach (var userId in userIds)
        {
            var result = await grantService.GrantTenantPermissionAsync(
                userId,
                tenantId,
                permissions,
                grantedBy,
                null,
                null,
                cancellationToken);
            results.Add(result);
        }

        return results;
    }

    public async Task<TenantPermission> JoinTenantAsync(
        Guid userId,
        Guid tenantId,
        Guid? invitedBy = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "User {UserId} joining tenant {TenantId}",
            userId,
            tenantId);

        var defaultPermissions = await queryService.GetTenantDefaultPermissionsAsync(tenantId, cancellationToken);

        return await grantService.GrantTenantPermissionAsync(
            userId,
            tenantId,
            defaultPermissions.ToArray(),
            invitedBy,
            null,
            "User joined tenant",
            cancellationToken);
    }

    public async Task<bool> LeaveTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "User {UserId} leaving tenant {TenantId}",
            userId,
            tenantId);

        var permissions = await queryService.GetTenantPermissionsAsync(userId, tenantId, cancellationToken);

        return await grantService.RevokeTenantPermissionAsync(
            userId,
            tenantId,
            permissions.ToArray(),
            cancellationToken);
    }
}
