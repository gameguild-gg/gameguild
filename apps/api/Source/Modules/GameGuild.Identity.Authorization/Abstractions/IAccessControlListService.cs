namespace GameGuild.Identity.Authorization;

/// <summary>
///     Manages Access Control Lists for <b>resource-level</b> access control.
///     Supports User, Role, Group, and Anonymous principals with deny-first evaluation.
/// </summary>
/// <remarks>
///     <para>
///         <b>Scope: RESOURCE-LEVEL ACCESS</b>
///     </para>
///     <para>
///         This service controls access to <b>specific resources</b> (e.g., "Can user X read/write Course #123?").
///         It determines what level of access (None, Read, ReadWrite, Admin) a user has on individual resources.
///     </para>
///     <para>
///         For <b>tenant-level operation permissions</b> (e.g., "Can user create courses in this tenant?"),
///         use <see cref="IPermissionQueryService"/> instead.
///     </para>
///     <para>
///         <b>Authorization Flow:</b>
///         <list type="number">
///             <item>First check tenant permission: Does user have "courses:edit" in tenant?</item>
///             <item>Then check resource ACL: Does user have access to this specific course?</item>
///         </list>
///         Both checks must pass for access to be granted.
///     </para>
/// </remarks>
public interface IAccessControlListService
{
    /// <summary>
    ///     Evaluates access for a subject on a specific resource using deny-first algorithm.
    ///     Checks all matching principals (user, roles, groups, anonymous) and returns the effective access level.
    /// </summary>
    /// <param name="subject">The ACL subject (user with roles/groups).</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The effective access level after applying deny-first rules.</returns>
    Task<AccessLevel> EvaluateAccessAsync(
        AclSubject subject,
        Guid tenantId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a subject has at least the specified access level.
    ///     Uses deny-first evaluation across all matching principals.
    /// </summary>
    /// <param name="subject">The ACL subject (user with roles/groups).</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="requiredLevel">The minimum required access level.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the subject has sufficient access.</returns>
    Task<bool> HasAccessAsync(
        AclSubject subject,
        Guid tenantId,
        string resourceType,
        string resourceId,
        AccessLevel requiredLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the access level for a user on a specific resource.
    ///     For backward compatibility - use EvaluateAccessAsync for full principal support.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's access level.</returns>
    Task<AccessLevel> GetAccessLevelAsync(
        Guid userId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Grants access to a resource for a principal.
    /// </summary>
    /// <param name="grantorId">The user granting access.</param>
    /// <param name="principalType">The type of principal (User, Role, Group, Anonymous).</param>
    /// <param name="principalId">The principal ID (null for Anonymous).</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="accessLevel">The access level to grant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task GrantAccessAsync(
        Guid grantorId,
        AclPrincipalType principalType,
        Guid? principalId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        AccessLevel accessLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Grants access to a resource for a user (backward-compatible overload).
    /// </summary>
    Task GrantAccessAsync(
        Guid grantorId,
        Guid granteeId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        AccessLevel accessLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Denies access to a resource for a principal.
    ///     Deny entries take precedence over allow entries.
    /// </summary>
    /// <param name="grantorId">The user creating the deny entry.</param>
    /// <param name="principalType">The type of principal (User, Role, Group, Anonymous).</param>
    /// <param name="principalId">The principal ID (null for Anonymous).</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="accessLevel">The access level to deny.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DenyAccessAsync(
        Guid grantorId,
        AclPrincipalType principalType,
        Guid? principalId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        AccessLevel accessLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Revokes access from a principal (removes both allow and deny entries).
    /// </summary>
    /// <param name="revokerId">The user revoking access.</param>
    /// <param name="principalType">The type of principal.</param>
    /// <param name="principalId">The principal ID (null for Anonymous).</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeAccessAsync(
        Guid revokerId,
        AclPrincipalType principalType,
        Guid? principalId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Revokes access from a user (backward-compatible overload).
    /// </summary>
    Task RevokeAccessAsync(
        Guid revokerId,
        Guid userId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a user has at least the specified access level.
    ///     For backward compatibility - use HasAccessAsync with AclSubject for full principal support.
    /// </summary>
    Task<bool> HasAccessAsync(
        Guid userId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        AccessLevel requiredLevel,
        CancellationToken cancellationToken = default);
}
