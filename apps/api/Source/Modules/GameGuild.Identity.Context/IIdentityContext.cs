using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Context;

/// <summary>
///     Interface for accessing the current identity context.
///     Provides unified access to the current user, tenant, and their permissions.
/// </summary>
/// <remarks>
///     <para>
///         <b>MIGRATION NOTE:</b> This interface is being superseded by <see cref="IActorContextAccessor"/>
///         which provides a more comprehensive and immutable security context.
///         New code should use <see cref="IActorContextAccessor"/> and <see cref="ActorContext"/> directly.
///     </para>
///     <para>
///         The <see cref="ActorContext"/> model provides:
///         <list type="bullet">
///             <item>Immutable, request-scoped security context</item>
///             <item>No HttpContext dependency (works in background jobs, tests)</item>
///             <item>Strongly-typed permission checks via typed Permission classes</item>
///             <item>Actor kind discrimination (User, Service, System, Webhook)</item>
///         </list>
///     </para>
///     <para>
///         <b>Migration path:</b>
///         <code>
///         // Before (legacy):
///         public class MyHandler(IIdentityContext identity)
///         {
///             var userId = identity.CurrentUserId;
///             var tenantId = identity.CurrentTenantId;
///         }
///         
///         // After (modern):
///         public class MyHandler(IActorContextAccessor actorContextAccessor)
///         {
///             var context = actorContextAccessor.ActorContext;
///             var userId = context.SubjectIdAsGuid;
///             var tenantId = context.TenantId;
///         }
///         </code>
///     </para>
/// </remarks>
[Obsolete("Use IActorContextAccessor for new code. This interface is maintained for backward compatibility. See ActorContext for the modern replacement.")]
public interface IIdentityContext
{
    /// <summary>
    ///     Gets the current user ID, or null if not authenticated.
    /// </summary>
    Guid? CurrentUserId { get; }

    /// <summary>
    ///     Gets the current tenant ID, or null if not in a tenant context.
    /// </summary>
    Guid? CurrentTenantId { get; }

    /// <summary>
    ///     Gets whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    ///     Gets the current user's email address.
    /// </summary>
    string? CurrentUserEmail { get; }

    /// <summary>
    ///     Gets the current user's display name.
    /// </summary>
    string? CurrentUserName { get; }

    /// <summary>
    ///     Gets the current user's roles.
    /// </summary>
    IReadOnlyList<string> CurrentUserRoles { get; }

    /// <summary>
    ///     Checks if the current user has the specified permission.
    /// </summary>
    /// <param name="permission">The permission to check.</param>
    /// <returns>True if the user has the permission, false otherwise.</returns>
    Task<bool> HasPermissionAsync(string permission);

    /// <summary>
    ///     Checks if the current user has any of the specified permissions.
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the user has any of the permissions, false otherwise.</returns>
    Task<bool> HasAnyPermissionAsync(params string[] permissions);

    /// <summary>
    ///     Checks if the current user has all of the specified permissions.
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the user has all of the permissions, false otherwise.</returns>
    Task<bool> HasAllPermissionsAsync(params string[] permissions);

    /// <summary>
    ///     Checks if the current user is in the specified role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the user is in the role, false otherwise.</returns>
    bool IsInRole(string role);
}
