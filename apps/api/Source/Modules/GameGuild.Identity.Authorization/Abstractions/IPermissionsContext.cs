using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Unified permission checking interface that combines user, tenant, and permission services.
///     Provides a convenient API for checking permissions in the current context.
/// </summary>
/// <remarks>
///     <para>
///         <b>ISP REFACTORING:</b> This interface now inherits from <see cref="IPermissionChecker"/>
///         and <see cref="IPermissionContextInfo"/> to comply with Interface Segregation Principle.
///         Clients can now depend on only the interface they need.
///     </para>
///     <para>
///         <b>MIGRATION NOTE:</b> This interface is being superseded by <see cref="IActorContextAccessor"/>
///         and <see cref="ActorContext"/> which provide pre-evaluated permissions.
///     </para>
///     <para>
///         Key differences:
///         <list type="bullet">
///             <item><see cref="ActorContext"/> contains pre-evaluated permissions loaded at request start</item>
///             <item><see cref="IPermissionsContext"/> fetches permissions on-demand from the database</item>
///         </list>
///     </para>
///     <para>
///         For simple permission checks, prefer <see cref="ActorContext.HasPermission(string)"/>.
///         For resource-level ACL checks, continue using <see cref="IPermissionChecker.HasResourcePermissionAsync"/>.
///     </para>
/// </remarks>
[Obsolete("Use IActorContextAccessor for new code. This interface is maintained for backward compatibility. " +
          "For focused dependencies, use IPermissionChecker or IPermissionContextInfo instead.")]
public interface IPermissionsContext : IPermissionChecker, IPermissionContextInfo
{
    // All members are now inherited from IPermissionChecker and IPermissionContextInfo.
    // This interface is kept for backward compatibility with existing code.
}
