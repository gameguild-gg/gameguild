using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Abstraction for accessing current user information from the request context.
///     Provides a testable interface for user-related claims and properties.
/// </summary>
/// <remarks>
///     <para>
///         <b>MIGRATION NOTE:</b> This interface is being superseded by <see cref="IActorContextAccessor"/>
///         which provides a more comprehensive and immutable security context.
///         New code should use <see cref="IActorContextAccessor"/> and <see cref="ActorContext"/> directly.
///     </para>
///     <para>
///         Existing code using <see cref="IUserContext"/> will continue to work via the
///         <see cref="ActorBasedUserContext"/> adapter.
///     </para>
/// </remarks>
[Obsolete("Use IActorContextAccessor for new code. This interface is maintained for backward compatibility.")]
public interface IUserContext
{
    /// <summary>
    ///     Gets the current user's ID
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    ///     Gets the current user's email address
    /// </summary>
    string? Email { get; }

    /// <summary>
    ///     Gets the current user's display name
    /// </summary>
    string? Name { get; }

    /// <summary>
    ///     Gets all claims for the current user as a dictionary
    /// </summary>
    IDictionary<string, object> Claims { get; }

    /// <summary>
    ///     Gets whether the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    ///     Gets all roles for the current user
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    ///     Checks if the current user is in a specific role
    /// </summary>
    /// <param name="role">The role name to check</param>
    /// <returns>True if user is in the role, false otherwise</returns>
    bool IsInRole(string role);
}
