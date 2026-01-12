namespace GameGuild.Identity.Authorization;

/// <summary>
///     Checks permission assignments for authorization handlers.
/// </summary>
public interface IAuthorizationPermissionService
{
    /// <summary>
    ///     Checks if a user has a specific permission in a tenant context.
    /// </summary>
    /// <param name="userId">The user ID (as Guid).</param>
    /// <param name="tenantId">The tenant ID (as Guid).</param>
    /// <param name="permission">The permission to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has the permission.</returns>
    Task<bool> HasPermissionAsync(
        Guid userId,
        Guid tenantId,
        string permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a user has all of the specified permissions in a tenant context.
    ///     This is more efficient than multiple HasPermissionAsync calls.
    /// </summary>
    /// <param name="userId">The user ID (as Guid).</param>
    /// <param name="tenantId">The tenant ID (as Guid).</param>
    /// <param name="permissions">The permissions to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating which permissions are present and which are missing.</returns>
    Task<PermissionCheckResult> HasAllPermissionsAsync(
        Guid userId,
        Guid tenantId,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a user has any of the specified permissions in a tenant context.
    ///     This is more efficient than multiple HasPermissionAsync calls.
    /// </summary>
    /// <param name="userId">The user ID (as Guid).</param>
    /// <param name="tenantId">The tenant ID (as Guid).</param>
    /// <param name="permissions">The permissions to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating which permissions are present.</returns>
    Task<PermissionCheckResult> HasAnyPermissionAsync(
        Guid userId,
        Guid tenantId,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all permissions for a user in a tenant context.
    /// </summary>
    /// <param name="userId">The user ID (as Guid).</param>
    /// <param name="tenantId">The tenant ID (as Guid).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of permission names the user has.</returns>
    Task<IReadOnlyList<string>> GetPermissionsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Result of a batch permission check.
/// </summary>
public sealed record PermissionCheckResult
{
    /// <summary>
    ///     Creates a successful result where all permissions were found.
    /// </summary>
    public static PermissionCheckResult AllPresent(IEnumerable<string> permissions) =>
        new()
        {
            HasAllRequired = true,
            HasAnyRequired = true,
            PresentPermissions = permissions.ToList(),
            MissingPermissions = []
        };

    /// <summary>
    ///     Creates a result with some permissions missing.
    /// </summary>
    public static PermissionCheckResult Partial(
        IEnumerable<string> present,
        IEnumerable<string> missing)
    {
        var presentList = present.ToList();
        var missingList = missing.ToList();
        return new PermissionCheckResult
        {
            HasAllRequired = false,
            HasAnyRequired = presentList.Count > 0,
            PresentPermissions = presentList,
            MissingPermissions = missingList
        };
    }

    /// <summary>
    ///     Creates a result where no permissions were found.
    /// </summary>
    public static PermissionCheckResult NonePresent(IEnumerable<string> permissions) =>
        new()
        {
            HasAllRequired = false,
            HasAnyRequired = false,
            PresentPermissions = [],
            MissingPermissions = permissions.ToList()
        };

    /// <summary>
    ///     Whether the user has all the requested permissions.
    /// </summary>
    public bool HasAllRequired { get; init; }

    /// <summary>
    ///     Whether the user has at least one of the requested permissions.
    /// </summary>
    public bool HasAnyRequired { get; init; }

    /// <summary>
    ///     The permissions that the user has.
    /// </summary>
    public IReadOnlyList<string> PresentPermissions { get; init; } = [];

    /// <summary>
    ///     The permissions that the user is missing.
    /// </summary>
    public IReadOnlyList<string> MissingPermissions { get; init; } = [];
}
