namespace GameGuild.Identity.Authorization;

/// <summary>
///     Checks a single permission for authorization handlers.
///     This is the simplest interface for permission checking (ISP compliance).
/// </summary>
/// <remarks>
///     <para>
///         This interface is for permission checks in the authorization subsystem.
///         It differs from <see cref="IPermissionChecker"/> which is for request-scoped permission checks.
///     </para>
/// </remarks>
public interface IAuthorizationSinglePermissionChecker
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
}

/// <summary>
///     Resolves all permissions for a user in a tenant context.
/// </summary>
public interface IAuthorizationPermissionResolver
{
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
///     Checks multiple permissions at once for authorization handlers.
///     This is for batch permission checks (more efficient than multiple single checks).
/// </summary>
public interface IAuthorizationBatchPermissionChecker
{
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
}

/// <summary>
///     Combined interface for authorization permission operations.
///     Inherits from focused interfaces for ISP compliance.
/// </summary>
/// <remarks>
///     <para>
///         This interface is a composition of smaller, focused interfaces:
///         <list type="bullet">
///             <item><see cref="IAuthorizationSinglePermissionChecker"/> - Single permission checks</item>
///             <item><see cref="IAuthorizationPermissionResolver"/> - Get all permissions</item>
///             <item><see cref="IAuthorizationBatchPermissionChecker"/> - Batch permission checks</item>
///         </list>
///     </para>
///     <para>
///         Prefer injecting the specific interface you need rather than this combined interface.
///         For example, use <see cref="IAuthorizationSinglePermissionChecker"/> if you only need single permission checks.
///     </para>
/// </remarks>
public interface IAuthorizationPermissionService : IAuthorizationSinglePermissionChecker, IAuthorizationPermissionResolver, IAuthorizationBatchPermissionChecker
{
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
