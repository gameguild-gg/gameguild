namespace GameGuild.Identity.Authorization;

/// <summary>
///     Stores security version numbers for user-specific cache invalidation.
/// </summary>
/// <remarks>
///     <para>
///         User security versions are incremented when user-specific permissions change.
///         This includes direct permission grants/revokes, role assignments, and group membership changes.
///     </para>
///     <para>
///         Used in conjunction with <see cref="ITenantSecurityVersionStore"/> for complete cache invalidation:
///         - Tenant version: invalidates all users in tenant when tenant-wide policies change
///         - User version: invalidates only the specific user when their permissions change
///     </para>
/// </remarks>
public interface IUserSecurityVersionStore
{
    /// <summary>
    ///     Gets the current security version for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current version number (0 if user has no version yet).</returns>
    Task<long> GetVersionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Increments the security version for a user (triggers cache invalidation).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new version number.</returns>
    Task<long> IncrementVersionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Increments the security version for multiple users atomically.
    /// </summary>
    /// <param name="userIds">The user IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IncrementVersionsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
}
