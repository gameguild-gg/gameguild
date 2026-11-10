using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Core.Compliance;

/// <summary>
///     Host composition-root bridge (issue #1206): connects the web client's
///     granular analytics/marketing consent surfaces (banner, settings privacy
///     form, "Cookie settings" withdrawal) to the Compliance.Consent module's
///     audited, versioned UserConsent records.
/// </summary>
/// <remarks>
///     Lives in the host — not the Consent module — because binding the
///     product's "Analytics Consent"/"Marketing Consent" policy names into an
///     endpoint contract would put product vocabulary in a platform module.
///     The acting user is resolved from the authenticated actor context (never
///     the request body); IP and user agent are captured server-side. Only
///     CHANGED decisions are written, so repeated banner views never spam
///     audit rows.
/// </remarks>
[Microsoft.AspNetCore.Http.Tags("compliance/consent")]
[ApiController]
[Route("api/compliance/consent")]
[Authorize]
public sealed class UserConsentBridgeController(ISender sender) : ControllerBase
{
    public sealed record UserConsentBridgeRequest(bool AnalyticsGranted, bool MarketingGranted, string? Method = null);

    [HttpPost("user-consent")]
    public async Task<ActionResult<RecordUserConsentEndpointCommand.Result>> RecordUserConsent(
        [FromBody] UserConsentBridgeRequest request, CancellationToken ct)
        => Ok(await sender.Send(new RecordUserConsentEndpointCommand(
            request.AnalyticsGranted, request.MarketingGranted, request.Method), ct).ConfigureAwait(false));
}
