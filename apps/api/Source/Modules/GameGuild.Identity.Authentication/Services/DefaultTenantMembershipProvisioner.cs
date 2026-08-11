using GameGuild.CQRS;
using GameGuild.Identity.Tenants;

namespace GameGuild.Identity.Authentication;

/// <summary>
/// Ensures that self-service registrations join the public GameGuild tenant.
/// </summary>
internal static class DefaultTenantMembershipProvisioner
{
    private const string MemberRole = "Member";
    private const string ExistingMembershipMessage = "User is already a member of this tenant";

    public static async Task EnsureAsync(ISender sender, Guid userId, CancellationToken cancellationToken)
    {
        var defaultTenant = await sender
            .Send(new GetDefaultTenantQuery(), cancellationToken)
            .ConfigureAwait(false);

        if (defaultTenant is null)
        {
            return;
        }

        var memberships = await sender
            .Send(new GetUserMembershipsQuery(userId, IncludeInactive: true), cancellationToken)
            .ConfigureAwait(false);

        var existingMembership = memberships.Memberships
            .FirstOrDefault(membership => membership.TenantId == defaultTenant.Id);

        if (existingMembership?.IsActive == true)
        {
            return;
        }

        // AddTenantMemberCommand reactivates inactive memberships. Preserve the existing
        // role so a cancelled SystemAdmin is never silently downgraded to Member.
        var role = string.IsNullOrWhiteSpace(existingMembership?.Role)
            ? MemberRole
            : existingMembership.Role.Trim();

        var result = await sender
            .Send(new AddTenantMemberCommand(defaultTenant.Id, userId, role), cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success && !string.Equals(result.Message, ExistingMembershipMessage, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unable to provision user {userId} in the default tenant: {result.Message ?? "unknown error"}.");
        }
    }
}
