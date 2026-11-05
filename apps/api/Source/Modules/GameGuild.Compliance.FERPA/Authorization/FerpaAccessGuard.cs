using GameGuild.Identity.Context.Actors;

namespace GameGuild.Compliance.FERPA;

/// <summary>
///     The acting party for a FERPA operation, resolved server-side from the authenticated
///     actor context (issue #1220 N4/N5). Never constructed from request-body data.
/// </summary>
public sealed record FerpaRequestActor(Guid ActorUserId, bool IsSystemAdmin, bool IsTenantAdmin, Guid? TenantId)
{
    public static FerpaRequestActor? From(ActorContext? context)
    {
        if (context is null || !context.IsAuthenticated)
        {
            return null;
        }

        var actorUserId = context.SubjectIdAsGuid;
        if (!actorUserId.HasValue)
        {
            return null;
        }

        return new FerpaRequestActor(
            actorUserId.Value,
            context.IsSystemAdmin,
            context.IsTenantAdmin && context.TenantId.HasValue,
            context.TenantId);
    }
}

/// <summary>
///     Thrown when the acting party is not permitted to perform a FERPA operation
///     (ownership or tenant-scope failure). The controller maps this to 403 — before
///     this existed, guard failures surfaced as unhandled 500s.
/// </summary>
public sealed class FerpaAuthorizationException(string message) : Exception(message);
