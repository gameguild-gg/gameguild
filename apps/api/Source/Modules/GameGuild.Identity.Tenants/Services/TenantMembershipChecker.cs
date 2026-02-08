using GameGuild.Identity.Authorization;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Implementation of <see cref="ITenantMembershipChecker"/> that delegates to the tenant member repository.
/// </summary>
/// <remarks>
///     This implementation uses the actual TenantMember entity to check membership,
///     rather than checking TenantPermission records (which is semantically different).
/// </remarks>
public sealed class TenantMembershipChecker(ITenantMemberRepository memberRepository) : ITenantMembershipChecker
{
    /// <inheritdoc />
    public async Task<bool> IsUserMemberOfTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByUserAndTenantAsync(userId, tenantId, cancellationToken).ConfigureAwait(false);
        
        // User is a member if:
        // 1. A TenantMember record exists
        // 2. The membership is active
        return member is { IsActive: true };
    }
}
