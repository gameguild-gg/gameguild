using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Notifications.Services.WhatsApp;

/// <summary>One resolved audience member of a broadcast segment (issue #55).</summary>
/// <param name="UserId">Tenant member user id.</param>
/// <param name="PhoneNumber">E.164 destination number from the user profile.</param>
public sealed record WhatsAppAudienceCandidate(Guid UserId, string PhoneNumber);

/// <summary>
///     Resolves the recipient segment for a broadcast campaign: tenant members (optionally
///     active-only, optionally filtered by membership role) who are active users with a
///     phone number on their profile.
/// </summary>
public interface IWhatsAppBroadcastAudienceResolver
{
    /// <summary>Resolves the candidate audience for the segment, newest membership first is not implied; order is unspecified.</summary>
    Task<IReadOnlyList<WhatsAppAudienceCandidate>> ResolveAsync(
        Guid tenantId,
        string? segmentRole,
        bool activeMembersOnly,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Default resolver: joins the tenant membership table with the user table through the
///     seams this module already references (GameGuild.Identity.Tenants/Users).
/// </summary>
public sealed class WhatsAppBroadcastAudienceResolver(IApplicationDbContext context) : IWhatsAppBroadcastAudienceResolver
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<WhatsAppAudienceCandidate>> ResolveAsync(
        Guid tenantId,
        string? segmentRole,
        bool activeMembersOnly,
        CancellationToken cancellationToken = default)
    {
        var memberships = context.Set<TenantMember>().IgnoreQueryFilters()
            .Where(m => m.TenantId == tenantId);

        if (activeMembersOnly)
        {
            memberships = memberships.Where(m => m.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(segmentRole))
        {
            memberships = memberships.Where(m => m.Role == segmentRole);
        }

        var users = context.Set<User>().IgnoreQueryFilters()
            .Where(u => u.IsActive && u.PhoneNumber != null && u.PhoneNumber != "");

        var candidates = await memberships
            .Join(users,
                m => m.UserId,
                u => u.Id,
                (m, u) => new { u.Id, Phone = u.PhoneNumber! })
            .GroupBy(c => c.Id)
            .Select(g => new { UserId = g.Key, Phone = g.First().Phone })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return candidates
            .Select(c => new WhatsAppAudienceCandidate(c.UserId, c.Phone.Trim()))
            .ToList();
    }
}
