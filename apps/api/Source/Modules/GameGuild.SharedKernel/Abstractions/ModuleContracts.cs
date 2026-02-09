namespace GameGuild;

/// <summary>
///     Contract for querying user information across module boundaries.
///     Modules that need user data should depend on this interface, not the Users module directly.
/// </summary>
/// <remarks>
///     This abstraction allows modules to access user information without creating
///     a direct dependency on the Users module, preventing circular references.
/// </remarks>
public interface IUserQueryService
{
    /// <summary>
    ///     Gets a user by their ID.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user info, or null if not found</returns>
    Task<UserInfo?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a user by their email address.
    /// </summary>
    /// <param name="email">The email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user info, or null if not found</returns>
    Task<UserInfo?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a user exists.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user exists</returns>
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Lightweight user information for cross-module queries.
///     Contains only the essential user data needed by other modules.
/// </summary>
public sealed record UserInfo(
    Guid Id,
    string Email,
    string Name,
    bool IsActive,
    Guid? TenantId = null);

/// <summary>
///     Contract for querying tenant information across module boundaries.
///     Modules that need tenant data should depend on this interface, not the Tenants module directly.
/// </summary>
public interface ITenantQueryService
{
    /// <summary>
    ///     Gets a tenant by their ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant info, or null if not found</returns>
    Task<TenantInfo?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a tenant by their slug.
    /// </summary>
    /// <param name="slug">The tenant slug</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant info, or null if not found</returns>
    Task<TenantInfo?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a tenant exists.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the tenant exists</returns>
    Task<bool> ExistsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Lightweight tenant information for cross-module queries.
///     Contains only the essential tenant data needed by other modules.
/// </summary>
public sealed record TenantInfo(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive);

/// <summary>
///     Contract for accessing the current request context across modules.
///     Provides access to the current user, tenant, and other request-scoped information.
/// </summary>
public interface IRequestContextAccessor
{
    /// <summary>
    ///     Gets the current user ID, if authenticated.
    /// </summary>
    Guid? CurrentUserId { get; }

    /// <summary>
    ///     Gets the current tenant ID, if in a tenant context.
    /// </summary>
    Guid? CurrentTenantId { get; }

    /// <summary>
    ///     Gets whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    ///     Gets whether the current request is in a tenant context.
    /// </summary>
    bool HasTenantContext { get; }

    /// <summary>
    ///     Gets the current user info, if authenticated.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The current user info, or null if not authenticated</returns>
    Task<UserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the current tenant info, if in a tenant context.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The current tenant info, or null if not in a tenant context</returns>
    Task<TenantInfo?> GetCurrentTenantAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Contract for authentication operations across module boundaries.
///     Modules that need to verify authentication should depend on this interface.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    ///     Validates an access token and returns the associated user ID.
    /// </summary>
    /// <param name="accessToken">The access token to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user ID if valid, null otherwise</returns>
    Task<Guid?> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a user has a specific permission.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permission">The permission to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has the permission</returns>
    Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default);
}
