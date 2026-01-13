namespace GameGuild.Identity.Authorization;

/// <summary>
///     Provides the current tenant context for authorization decisions.
/// </summary>
public interface IAuthorizationTenantContext
{
    /// <summary>
    ///     Gets the current tenant ID as a strongly-typed Guid.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property returns <c>null</c> when no tenant context is available.
    ///         Authorization decisions should fail-closed when tenant is null.
    ///     </para>
    /// </remarks>
    Guid? TenantId { get; }

    /// <summary>
    ///     Gets whether a tenant has been resolved.
    /// </summary>
    bool HasTenant => TenantId.HasValue;
}
