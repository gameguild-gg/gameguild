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

        if (memberships.Memberships.Any(membership => membership.TenantId == defaultTenant.Id))
        {
            return;
        }

        var result = await sender
            .Send(new AddTenantMemberCommand(defaultTenant.Id, userId, MemberRole), cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success && !string.Equals(result.Message, ExistingMembershipMessage, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unable to provision user {userId} in the default tenant: {result.Message ?? "unknown error"}.");
        }
    }
}
