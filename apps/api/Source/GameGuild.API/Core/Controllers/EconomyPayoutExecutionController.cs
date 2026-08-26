using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using GameGuild.API.Setup;
using GameGuild.Economy.Payouts;
using GameGuild.Economy.Risk;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Controllers;

public sealed record ReserveApprovedPayoutExecutionRequest(
    string JurisdictionCode,
    Guid RiskDecisionId,
    string OperationFingerprint);

public sealed record DispatchPayoutExecutionRequest(
    long ExpectedVersion,
    string JurisdictionCode,
    Guid RiskDecisionId,
    string OperationFingerprint);

public sealed record EconomyPayoutExecutionOperationDto(
    Guid Id,
    Guid PayeeId,
    Guid WalletId,
    long HardCoinUnits,
    PayoutOperationState State,
    long Version,
    long FencingToken,
    long KillSwitchEpoch,
    long ReserveVersion,
    long ReserveAuthorizationEpoch,
    long PolicyVersion,
    Guid RiskDecisionId,
    string DestinationHash,
    string? ProviderPayoutId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static EconomyPayoutExecutionOperationDto From(PayoutOperation operation) => new(
        operation.Id,
        operation.PayeeId,
        operation.WalletId.Value,
        operation.Amount.Units,
        operation.State,
        operation.Version,
        operation.FencingToken,
        operation.KillSwitchEpoch,
        operation.ReserveVersion.Value,
        operation.ReserveAuthorizationEpoch,
        operation.PolicyVersion.Value,
        operation.RiskDecisionId,
        operation.DestinationHash,
        operation.ProviderPayoutId,
        operation.CreatedAt,
        operation.UpdatedAt);
}

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/economy/payouts")]
[Tags("economy")]
[Authorize]
public sealed class EconomyPayoutAccountController(
    IDurablePayoutApplicationService payouts,
    IActorContextAccessor actorContextAccessor) : BaseApiController
{
    [HttpPost("onboarding")]
    [EndpointSummary("Create or refresh my payout provider onboarding")]
    [ProducesResponseType(typeof(ConnectOnboardingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateOrRefreshOnboarding(CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out var actorId)) return Forbid();
        try
        {
            return Ok(await payouts.CreateOrRefreshAccountAsync(
                tenantId, actorId, cancellationToken).ConfigureAwait(false));
        }
        catch (PayoutExecutionDisabledException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, exception.Message);
        }
    }

    [HttpGet("account")]
    [EndpointSummary("Get my payout provider account readiness")]
    [ProducesResponseType(typeof(ConnectAccountSnapshot), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAccount(CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out var actorId)) return Forbid();
        try
        {
            return Ok(await payouts.GetAccountAsync(tenantId, actorId, cancellationToken).ConfigureAwait(false));
        }
        catch (PayoutEligibilityException)
        {
            return NotFound();
        }
        catch (PayoutExecutionDisabledException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, exception.Message);
        }
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
[Route("api/v{version:apiVersion}/admin/economy/payout-requests")]
[Tags("economy-administration")]
[Authorize]
public sealed class EconomyPayoutExecutionAdministrationController(
    IDurablePayoutApplicationService payouts,
    IActorContextAccessor actorContextAccessor,
    TimeProvider timeProvider) : BaseApiController
{
    private static readonly TimeSpan ReauthenticationLifetime = TimeSpan.FromMinutes(5);

    [HttpPost("{requestId:guid}/reserve")]
    [EndpointSummary("Reserve FIFO funds for a fully approved payout request")]
    [EndpointDescription("Tenant and actor authority come exclusively from the authenticated actor context. Fresh MFA and the full capability control plane are required.")]
    [ProducesResponseType(typeof(EconomyPayoutExecutionOperationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Reserve(
        Guid requestId,
        [FromBody] ReserveApprovedPayoutExecutionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteProtectedAsync(
            request.OperationFingerprint,
            (tenantId, actorId, evidence) => payouts.ReserveApprovedAsync(
                new ReserveApprovedPayoutCommand(
                    tenantId, actorId, requestId, request.JurisdictionCode,
                    request.RiskDecisionId, request.OperationFingerprint, evidence),
                cancellationToken));
    }

    [HttpPost("operations/{operationId:guid}/dispatch")]
    [EndpointSummary("Atomically authorize and enqueue an approved payout dispatch")]
    [ProducesResponseType(typeof(EconomyPayoutExecutionOperationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Dispatch(
        Guid operationId,
        [FromBody] DispatchPayoutExecutionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteProtectedAsync(
            request.OperationFingerprint,
            (tenantId, actorId, evidence) => payouts.DispatchAsync(
                new DispatchPayoutOperationCommand(
                    tenantId, actorId, operationId, request.ExpectedVersion,
                    request.JurisdictionCode, request.RiskDecisionId,
                    request.OperationFingerprint, evidence),
                cancellationToken));
    }

    [HttpPost("operations/{operationId:guid}/reconcile")]
    [EndpointSummary("Reconcile an in-flight payout directly with its provider")]
    [ProducesResponseType(typeof(EconomyPayoutExecutionOperationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reconcile(Guid operationId, CancellationToken cancellationToken)
    {
        if (!TryOperator(out var tenantId, out var actorId, out _)) return Forbid();
        return await ExecuteAsync(
            () => payouts.ReconcileAsync(
                new ReconcilePayoutOperationCommand(tenantId, actorId, operationId), cancellationToken))
            .ConfigureAwait(false);
    }

    [HttpGet("operations")]
    [EndpointSummary("List tenant-scoped payout execution operations")]
    [ProducesResponseType(typeof(IReadOnlyList<EconomyPayoutExecutionOperationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult List([FromQuery] int take = 100)
    {
        if (!TryOperator(out var tenantId, out _, out _)) return Forbid();
        if (take is < 1 or > 100) return BadRequest("Take must be between 1 and 100.");
        return Ok(payouts.List(tenantId, take).Select(EconomyPayoutExecutionOperationDto.From).ToArray());
    }

    [HttpGet("operations/{operationId:guid}")]
    [EndpointSummary("Get a tenant-scoped payout execution operation")]
    [ProducesResponseType(typeof(EconomyPayoutExecutionOperationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Get(Guid operationId)
    {
        if (!TryOperator(out var tenantId, out _, out _)) return Forbid();
        try
        {
            return Ok(EconomyPayoutExecutionOperationDto.From(payouts.Get(tenantId, operationId)));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private async Task<IActionResult> ExecuteProtectedAsync(
        string operationFingerprint,
        Func<Guid, Guid, ReauthenticationEvidence, ValueTask<PayoutOperation>> action)
    {
        if (!TryOperator(out var tenantId, out var actorId, out var actor)) return Forbid();
        if (!TryCreateReauthentication(actor, operationFingerprint, timeProvider.GetUtcNow(), out var evidence))
            return Conflict("A fresh MFA-authenticated session is required for payout value movement.");
        return await ExecuteAsync(() => action(tenantId, actorId, evidence)).ConfigureAwait(false);
    }

    private static async Task<IActionResult> ExecuteAsync(Func<ValueTask<PayoutOperation>> action)
    {
        try
        {
            return new OkObjectResult(EconomyPayoutExecutionOperationDto.From(await action().ConfigureAwait(false)));
        }
        catch (KeyNotFoundException)
        {
            return new NotFoundResult();
        }
        catch (Exception exception) when (exception is PayoutEligibilityException or
                                          PayoutExecutionDisabledException or
                                          PayoutStaleCommandException or
                                          PayoutProviderBindingException or
                                          ReauthenticationEvidenceException or
                                          EconomyCapabilityAuthorizationException)
        {
            return new ConflictObjectResult(exception.Message);
        }
    }

    private bool TryOperator(out Guid tenantId, out Guid actorId, out ActorContext actor)
    {
        actor = actorContextAccessor.ActorContext;
        tenantId = Guid.Empty;
        actorId = Guid.Empty;
        if (!actor.IsAuthenticated || !actor.TenantId.HasValue || !actor.SubjectIdAsGuid.HasValue ||
            !actor.HasPermission(EconomyPermission.Keys.OperatePayouts))
            return false;
        tenantId = actor.TenantId.Value;
        actorId = actor.SubjectIdAsGuid.Value;
        return true;
    }

    internal static bool TryCreateReauthentication(
        ActorContext actor,
        string operationFingerprint,
        DateTimeOffset now,
        out ReauthenticationEvidence evidence)
    {
        evidence = null!;
        if (!actor.IsMfaVerified || actor.SubjectIdAsGuid is not { } actorId ||
            actor.TenantId is not { } tenantId || actor.TypedAttributes.AuthenticatedAt is not { } issuedAt ||
            issuedAt > now)
            return false;
        var sessionBinding = actor.TypedAttributes.SessionId ?? actor.TypedAttributes.TokenId;
        if (string.IsNullOrWhiteSpace(sessionBinding) || string.IsNullOrWhiteSpace(operationFingerprint))
            return false;
        var expiresAt = issuedAt.Add(ReauthenticationLifetime);
        if (actor.TypedAttributes.TokenExpiresAt is { } tokenExpiresAt && tokenExpiresAt < expiresAt)
            expiresAt = tokenExpiresAt;
        if (expiresAt <= now) return false;
        var evidenceHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',
            tenantId.ToString("N"),
            actorId.ToString("N"),
            sessionBinding.Trim(),
            issuedAt.UtcTicks.ToString(CultureInfo.InvariantCulture),
            expiresAt.UtcTicks.ToString(CultureInfo.InvariantCulture),
            operationFingerprint.Trim()))));
        evidence = new ReauthenticationEvidence(
            actorId,
            ProtectedOperationKind.Payout,
            operationFingerprint.Trim(),
            ReauthenticationAssurance.MultiFactor,
            issuedAt,
            expiresAt,
            evidenceHash);
        return true;
    }
}

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/integrations/economy/stripe-connect")]
[Tags("economy-integrations")]
[ApiController]
public sealed class EconomyStripeConnectWebhookController(
    IStripeConnectWebhookNormalizer normalizer,
    IDurablePayoutApplicationService payouts,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EconomyPayoutExecutionOperationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ingest(CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("Stripe-Signature", out var signature) ||
            string.IsNullOrWhiteSpace(signature.ToString()))
            return BadRequest("Stripe-Signature is required.");
        await using var payload = new MemoryStream();
        await Request.Body.CopyToAsync(payload, cancellationToken).ConfigureAwait(false);
        var providerEvent = await normalizer.NormalizeAsync(
            payload.ToArray(), signature.ToString(), timeProvider.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);
        var operation = await payouts.ApplyProviderEventAsync(providerEvent, cancellationToken)
            .ConfigureAwait(false);
        return Ok(EconomyPayoutExecutionOperationDto.From(operation));
    }
}
