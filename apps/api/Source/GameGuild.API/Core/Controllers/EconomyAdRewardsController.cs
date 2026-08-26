using Asp.Versioning;
using GameGuild.Economy.AdRewards;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;
using GameGuild.Economy.Risk;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Controllers;

public sealed record StartMyAdRewardSessionRequest(
    string Network,
    string CreativeId,
    string DeviceRiskHash,
    string IpRiskHash,
    string AsnRiskHash,
    double RequiredDurationSeconds,
    string IdempotencyKey);
public sealed record CompleteMyAdRewardSessionRequest(
    string Token,
    AdPlaybackEvidence Playback,
    ProviderCompletionProof? ProviderProof,
    string IdempotencyKey,
    Guid RiskDecisionId,
    string OperationFingerprint,
    string JurisdictionCode);
public sealed record ConfirmMyDeferredAdRewardRequest(
    string IdempotencyKey,
    Guid RiskDecisionId,
    string OperationFingerprint,
    string JurisdictionCode);

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/economy/ad-rewards")]
[Tags("economy-ad-rewards")]
[Authorize]
public sealed class EconomyAdRewardsController(
    IDurableAdRewardSessionService sessions,
    IDurableAdRewardCompletionService completions,
    IDurableDeferredAdRewardService deferred,
    IDurableAdRewardSessionReader reader,
    IEconomyWalletDirectory wallets,
    IActorContextAccessor actorContextAccessor,
    TimeProvider timeProvider) : BaseApiController
{
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(DurableAdRewardSessionResult), StatusCodes.Status201Created)]
    public async Task<IActionResult> Start(
        [FromBody] StartMyAdRewardSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out var actorId)) return Forbid();
        ArgumentNullException.ThrowIfNull(request);
        var wallet = await wallets.GetOwnerWalletAsync(tenantId, actorId, cancellationToken)
            .ConfigureAwait(false);
        var result = await sessions.StartAsync(new StartDurableAdRewardSessionRequest(
            tenantId,
            actorId,
            wallet.WalletId,
            request.Network,
            request.CreativeId,
            request.DeviceRiskHash,
            request.IpRiskHash,
            request.AsnRiskHash,
            TimeSpan.FromSeconds(request.RequiredDurationSeconds),
            new IdempotencyKey(request.IdempotencyKey),
            timeProvider.GetUtcNow()), cancellationToken).ConfigureAwait(false);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("sessions/{sessionId:guid}/complete")]
    [ProducesResponseType(typeof(DurableAdRewardCompletionResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(
        Guid sessionId,
        [FromBody] CompleteMyAdRewardSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out var actorId)) return Forbid();
        ArgumentNullException.ThrowIfNull(request);
        if (request.ProviderProof is not null && request.ProviderProof.SessionId != sessionId)
            return BadRequest("Provider proof is bound to another ad reward session.");
        var result = await completions.CompleteAsync(new CompleteDurableAdRewardSessionRequest(
            tenantId,
            actorId,
            EconomySubjectReference.ForUser(tenantId, actorId),
            request.JurisdictionCode,
            new SignedAdRewardSession(request.Token),
            request.Playback,
            request.ProviderProof,
            new IdempotencyKey(request.IdempotencyKey),
            request.RiskDecisionId,
            request.OperationFingerprint,
            timeProvider.GetUtcNow()), cancellationToken).ConfigureAwait(false);
        return result.SessionId == sessionId
            ? Ok(result)
            : Conflict("The signed token is bound to another ad reward session.");
    }

    [HttpGet("sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(DurableAdRewardSessionStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Status(Guid sessionId, CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out var actorId)) return Forbid();
        var status = await reader.FindAsync(tenantId, actorId, sessionId, cancellationToken)
            .ConfigureAwait(false);
        return status is null ? NotFound() : Ok(status);
    }

    [HttpPost("sessions/{sessionId:guid}/confirm-deferred")]
    [ProducesResponseType(typeof(DurableAdRewardCompletionResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmDeferred(
        Guid sessionId,
        [FromBody] ConfirmMyDeferredAdRewardRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out var actorId)) return Forbid();
        ArgumentNullException.ThrowIfNull(request);
        return Ok(await deferred.ConfirmAsync(new ConfirmDeferredAdRewardRequest(
            tenantId,
            actorId,
            sessionId,
            EconomySubjectReference.ForUser(tenantId, actorId),
            request.JurisdictionCode,
            new IdempotencyKey(request.IdempotencyKey),
            request.RiskDecisionId,
            request.OperationFingerprint,
            timeProvider.GetUtcNow()), cancellationToken).ConfigureAwait(false));
    }

    private bool TryActor(out Guid tenantId, out Guid actorId)
    {
        tenantId = Guid.Empty;
        actorId = Guid.Empty;
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || !actor.TenantId.HasValue || !actor.SubjectIdAsGuid.HasValue)
            return false;
        tenantId = actor.TenantId.Value;
        actorId = actor.SubjectIdAsGuid.Value;
        return true;
    }
}

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/economy/ad-rewards")]
[Tags("economy-administration")]
[Authorize]
public sealed class EconomyAdRewardsAdministrationController(
    IDurableAdRewardReportService reports,
    IDurableAdRewardReportReader reportReader,
    IActorContextAccessor actorContextAccessor,
    TimeProvider timeProvider) : BaseApiController
{
    [HttpPost("reports")]
    [ProducesResponseType(typeof(DurableAdProviderReportImportResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportReport(
        [FromBody] AdProviderReport report,
        CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || !actor.TenantId.HasValue ||
            !actor.HasPermission(EconomyPermission.Keys.OperateAdRewards))
            return Forbid();
        return Ok(await reports.ImportAsync(
            new ImportDurableAdProviderReportRequest(
                actor.TenantId.Value,
                report,
                timeProvider.GetUtcNow()),
            cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("reports")]
    [ProducesResponseType(typeof(IReadOnlyList<DurableAdProviderReportStatus>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListReports(
        [FromQuery] string? network = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || !actor.TenantId.HasValue ||
            !actor.HasPermission(EconomyPermission.Keys.OperateAdRewards))
            return Forbid();
        return Ok(await reportReader.ListAsync(
            actor.TenantId.Value, network, limit, cancellationToken).ConfigureAwait(false));
    }
}
