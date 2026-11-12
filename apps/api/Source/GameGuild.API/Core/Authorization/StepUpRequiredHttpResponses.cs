using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Authorization;

/// <summary>
///     Shared mapping from <see cref="StepUpRequiredException"/> to the structured HTTP 423
///     step-up trigger (issue #1191). Sensitive actions whose step-up receipt is missing or
///     invalid answer with a <see cref="StepUpAuthenticationRequiredResponse"/> body — the
///     exact operation binding to re-challenge and the account's available MFA methods —
///     instead of a generic 403/409.
/// </summary>
public static class StepUpRequiredHttpResponses
{
    public static async Task<IActionResult> StepUpRequiredAsync(
        this IStepUpRequiredResponder responder,
        GameGuild.Identity.Context.Actors.IActorContextAccessor actors,
        StepUpRequiredException exception,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(responder);
        ArgumentNullException.ThrowIfNull(actors);
        ArgumentNullException.ThrowIfNull(exception);

        var actor = actors.ActorContext;
        var payload = await responder
            .BuildAsync(actor.SubjectIdAsGuid ?? Guid.Empty, exception, cancellationToken)
            .ConfigureAwait(false);
        return new ObjectResult(payload) { StatusCode = StatusCodes.Status423Locked };
    }
}
