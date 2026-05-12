using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Query to retrieve a single invitation visible to the current user.
/// </summary>
/// <param name="InvitationId">The invitation ID.</param>
public sealed record GetResourceInvitationQuery(Guid InvitationId) : IQuery<GetResourceInvitationResponse>;

/// <summary>
///     Response containing a single invitation.
/// </summary>
public sealed record GetResourceInvitationResponse
{
    public required ResourceInvitationDto Invitation { get; init; }
}

/// <summary>
///     Query to retrieve all pending invitations for the authenticated user's email address.
/// </summary>
public sealed record GetPendingResourceInvitationsQuery : IQuery<GetPendingResourceInvitationsResponse>;

/// <summary>
///     Response containing pending invitations for the current user.
/// </summary>
public sealed record GetPendingResourceInvitationsResponse
{
    public required List<ResourceInvitationDto> Invitations { get; init; }

    public int TotalCount => Invitations.Count;
}

/// <summary>
///     Handler for GetResourceInvitationQuery.
/// </summary>
public sealed class GetResourceInvitationQueryHandler(
    IApplicationDbContext dbContext,
    IActorContextAccessor actorContextAccessor,
    ILogger<GetResourceInvitationQueryHandler> logger)
    : IQueryHandler<GetResourceInvitationQuery, GetResourceInvitationResponse>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<GetResourceInvitationResponse> Handle(GetResourceInvitationQuery request, CancellationToken cancellationToken)
    {
        var invitation = await dbContext.Set<ResourceInvitation>()
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.InvitationId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException("ResourceInvitation", request.InvitationId);

        var userEmail = Actor.TypedAttributes.Email;
        var canView = Actor.IsSystemAdmin ||
                      (Actor.IsTenantAdmin && Actor.TenantId.HasValue && Actor.TenantId.Value == invitation.TenantId.Value) ||
                      (!string.IsNullOrWhiteSpace(userEmail) && ResourceInvitationQueryMappings.EmailsMatch(invitation.Email, userEmail));

        if (!canView)
        {
            logger.LogWarning(
                "User {UserId} attempted to view invitation {InvitationId} without authorization",
                Actor.SubjectIdAsGuid,
                request.InvitationId);

            throw new UnauthorizedAccessException("You do not have permission to view this invitation");
        }

        return new GetResourceInvitationResponse
        {
            Invitation = ResourceInvitationQueryMappings.MapInvitation(invitation)
        };
    }
}

/// <summary>
///     Handler for GetPendingResourceInvitationsQuery.
/// </summary>
public sealed class GetPendingResourceInvitationsQueryHandler(
    IApplicationDbContext dbContext,
    IActorContextAccessor actorContextAccessor)
    : IQueryHandler<GetPendingResourceInvitationsQuery, GetPendingResourceInvitationsResponse>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<GetPendingResourceInvitationsResponse> Handle(GetPendingResourceInvitationsQuery request, CancellationToken cancellationToken)
    {
        var email = Actor.TypedAttributes.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException("Authenticated user must have an email address to view invitations");
        }

        var normalizedEmail = email.Trim().ToUpperInvariant();

        var invitations = await dbContext.Set<ResourceInvitation>()
            .AsNoTracking()
            .Where(invitation => invitation.Status == InvitationStatus.Pending &&
                                 invitation.Email.ToUpper() == normalizedEmail &&
                                 (invitation.ExpiresAt == null || invitation.ExpiresAt > SystemClock.UtcNow))
            .OrderByDescending(invitation => invitation.InvitedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new GetPendingResourceInvitationsResponse
        {
            Invitations = invitations.Select(ResourceInvitationQueryMappings.MapInvitation).ToList()
        };
    }
}

internal static class ResourceInvitationQueryMappings
{
    internal static ResourceInvitationDto MapInvitation(ResourceInvitation invitation)
    {
        return new ResourceInvitationDto(
            invitation.Id,
            invitation.TenantId.Value,
            invitation.Email,
            invitation.ResourceType,
            invitation.ResourceId,
            invitation.Permissions,
            invitation.Message,
            invitation.InvitedByUserName,
            invitation.InvitedAt,
            invitation.ExpiresAt,
            invitation.Status.ToString());
    }

    internal static bool EmailsMatch(string left, string right)
    {
        return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
