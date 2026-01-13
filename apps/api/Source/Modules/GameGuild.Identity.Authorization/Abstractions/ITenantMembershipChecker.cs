namespace GameGuild.Identity.Authorization;

/// <summary>
///     Abstraction for checking tenant membership.
///     This interface is implemented by the Tenants module to provide actual membership checks.
/// </summary>
/// <remarks>
///     <para>
///         <b>Why this abstraction exists:</b>
///         The Authorization module needs to check if a user is a member of a tenant,
///         but it should not depend directly on the Tenants module (SRP, dependency inversion).
///     </para>
///     <para>
///         <b>Implementation:</b>
///         The Tenants module registers an implementation that delegates to ITenantMemberRepository.
///         If no implementation is registered, a fallback is used that always returns false (fail-closed).
///     </para>
/// </remarks>
public interface ITenantMembershipChecker
{
    /// <summary>
    ///     Check if a user is an active member of a tenant.
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <param name="tenantId">The tenant ID to check membership in</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user is an active member of the tenant, false otherwise</returns>
    /// <remarks>
    ///     This checks actual tenant membership (TenantMember entity), not permissions.
    ///     A user may have permissions in a tenant without being a member (e.g., global permissions).
    /// </remarks>
    Task<bool> IsUserMemberOfTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Fail-closed implementation of <see cref="ITenantMembershipChecker"/>.
///     Always returns false - used when no actual implementation is registered.
/// </summary>
/// <remarks>
///     <b>Security:</b> This ensures that if the Tenants module is not loaded or
///     the implementation is not registered, membership checks fail closed (deny access).
/// </remarks>
public sealed class FailClosedTenantMembershipChecker : ITenantMembershipChecker
{
    /// <inheritdoc />
    public Task<bool> IsUserMemberOfTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // SECURITY: Fail closed - no implementation means no membership
        return Task.FromResult(false);
    }
}
