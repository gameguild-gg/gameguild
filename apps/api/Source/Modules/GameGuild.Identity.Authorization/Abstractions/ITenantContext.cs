using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Abstraction for accessing current tenant information from the request context.
///     Provides a testable interface for tenant-related claims and properties.
/// </summary>
/// <remarks>
///     <para>
///         <b>MIGRATION NOTE:</b> This interface is being superseded by <see cref="IActorContextAccessor"/>
///         which provides a more comprehensive and immutable security context.
///         New code should use <see cref="IActorContextAccessor"/> and <see cref="ActorContext"/> directly.
///     </para>
///     <para>
///         Existing code using <see cref="ITenantContext"/> will continue to work via the
///         <see cref="ActorBasedTenantContext"/> adapter.
///     </para>
/// </remarks>
[Obsolete("Use IActorContextAccessor for new code. This interface is maintained for backward compatibility.")]
public interface ITenantContext
{
    /// <summary>
    ///     Gets the current tenant ID
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    ///     Gets the current tenant name
    /// </summary>
    string? TenantName { get; }

    /// <summary>
    ///     Gets tenant-specific settings as a dictionary
    /// </summary>
    IDictionary<string, object> Settings { get; }

    /// <summary>
    ///     Gets whether the current tenant is active
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    ///     Gets the subscription plan for the current tenant
    /// </summary>
    string? SubscriptionPlan { get; }
}
