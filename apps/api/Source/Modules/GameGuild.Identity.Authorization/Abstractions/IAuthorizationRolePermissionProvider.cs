namespace GameGuild.Identity.Authorization;

/// <summary>
/// Supplies effective permission keys from role systems implemented by higher-level modules.
/// </summary>
public interface IAuthorizationRolePermissionProvider
{
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
