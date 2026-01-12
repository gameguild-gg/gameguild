namespace GameGuild.Identity.Authorization;

/// <summary>
///     Interface for accessing current user/tenant identity and role information.
///     Provides context about who is making the current request.
/// </summary>
/// <remarks>
///     <para>
///         <b>ISP Compliance:</b> This interface is segregated from <see cref="IPermissionChecker"/>
///         to allow clients to depend only on context info without needing permission checking.
///     </para>
///     <para>
///         <b>MIGRATION NOTE:</b> For new code, prefer using <see cref="GameGuild.Identity.Context.Actors.IActorContextAccessor"/>
///         which provides a richer, immutable context model.
///     </para>
/// </remarks>
public interface IPermissionContextInfo
{
    /// <summary>
    ///     Gets the current user ID from context (null if anonymous).
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    ///     Gets the current tenant ID from context (null if no tenant selected).
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    ///     Gets whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    ///     Gets whether the current user is a system administrator.
    ///     System admins have unrestricted access across all tenants.
    /// </summary>
    bool IsSystemAdmin { get; }

    /// <summary>
    ///     Gets whether the current user is an administrator of the current tenant.
    ///     Tenant admins have full access within their tenant.
    /// </summary>
    bool IsTenantAdmin { get; }
}
