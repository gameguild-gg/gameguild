using Asp.Versioning;
using GameGuild.API.Authorization;
using GameGuild.API.Setup;
using GameGuild.Economy.Reserves;
using GameGuild.Economy.Risk;
using GameGuild.Economy.Treasury;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Controllers;

public sealed record ProposeTreasuryWithdrawalRequest(
    DateOnly PeriodStart,
    long AmountUnits,
    string DestinationHash,
    string JurisdictionCode,
    Guid RiskDecisionId,
    string OperationFingerprint,
    string IdempotencyKey);

public sealed record ApproveTreasuryWithdrawalRequest(long ExpectedVersion, string StepUpReceipt);

public sealed record DispatchTreasuryWithdrawalRequest(
    long ExpectedVersion,
    Guid RiskDecisionId,
    string OperationFingerprint,
    string StepUpReceipt);

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/economy/treasury/withdrawals")]
[Tags("economy-treasury-administration")]
[Authorize]
public sealed class EconomyTreasuryAdministrationController(
    IDurableAdminWithdrawalApplicationService withdrawals,
    IEconomyStepUpExecutor stepUp,
    IActorContextAccessor actorContextAccessor) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminWithdrawalRun>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult List([FromQuery] int limit = 100)
    {
        if (!TryActor(out var tenantId, out _)) return Forbid();
        if (limit is <= 0 or > 500) return BadRequest("Limit must be between 1 and 500.");
        return Ok(withdrawals.List(tenantId, limit));
    }

    [HttpGet("{runId:guid}")]
    [ProducesResponseType(typeof(AdminWithdrawalRun), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Get(Guid runId)
    {
        if (!TryActor(out var tenantId, out _)) return Forbid();
        try { return Ok(withdrawals.Get(tenantId, runId)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpGet("{runId:guid}/audit")]
    [ProducesResponseType(typeof(AdminWithdrawalAuditView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Audit(Guid runId)
    {
        if (!TryActor(out var tenantId, out _)) return Forbid();
        try { return Ok(withdrawals.Audit(tenantId, runId)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(AdminWithdrawalRun), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Propose(
        [FromBody] ProposeTreasuryWithdrawalRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out var actorId)) return Forbid();
        ArgumentNullException.ThrowIfNull(request);
        return await ExecuteAsync(async () => await withdrawals.ProposeAsync(
            new ProposeAdminWithdrawalCommand(
                tenantId,
                actorId,
                request.PeriodStart,
                request.AmountUnits,
                request.DestinationHash,
                request.JurisdictionCode,
                request.RiskDecisionId,
                request.OperationFingerprint,
                request.IdempotencyKey),
            cancellationToken).ConfigureAwait(false), created: true).ConfigureAwait(false);
    }

    [HttpPost("{runId:guid}/approve")]
    [ProducesResponseType(typeof(AdminWithdrawalRun), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(
        Guid runId,
        [FromBody] ApproveTreasuryWithdrawalRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out var actorId)) return Forbid();
        ArgumentNullException.ThrowIfNull(request);
        var operation = EconomyStepUpOperation.Create(
            "economy.treasury.approve",
            $"treasury-withdrawal:{runId:N}",
            runId.ToString("N"),
            request.ExpectedVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return await ExecuteAsync(() => stepUp.ExecuteAsync(
            operation,
            request.StepUpReceipt,
            (_, token) => withdrawals.ApproveAsync(
                new ApproveAdminWithdrawalCommand(
                    tenantId, actorId, runId, request.ExpectedVersion), token),
            cancellationToken)).ConfigureAwait(false);
    }

    [HttpPost("{runId:guid}/dispatch")]
    [ProducesResponseType(typeof(AdminWithdrawalRun), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Dispatch(
        Guid runId,
        [FromBody] DispatchTreasuryWithdrawalRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out var actorId)) return Forbid();
        ArgumentNullException.ThrowIfNull(request);
        var operation = EconomyStepUpOperation.Create(
            "economy.treasury.dispatch",
            $"treasury-withdrawal:{runId:N}",
            runId.ToString("N"),
            request.ExpectedVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            request.RiskDecisionId.ToString("N"),
            request.OperationFingerprint);
        return await ExecuteAsync(() => stepUp.ExecuteAsync(
            operation,
            request.StepUpReceipt,
            (_, token) => withdrawals.DispatchAsync(
                new DispatchAdminWithdrawalCommand(
                    tenantId,
                    actorId,
                    runId,
                    request.ExpectedVersion,
                    request.RiskDecisionId,
                    request.OperationFingerprint),
                token),
            cancellationToken)).ConfigureAwait(false);
    }

    [HttpPost("{runId:guid}/reconcile")]
    [ProducesResponseType(typeof(AdminWithdrawalRun), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Reconcile(Guid runId, CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out var actorId)) return Forbid();
        return await ExecuteAsync(() => withdrawals.ReconcileAsync(
            new ReconcileAdminWithdrawalCommand(tenantId, actorId, runId), cancellationToken))
            .ConfigureAwait(false);
    }

    private async Task<IActionResult> ExecuteAsync(
        Func<Task<AdminWithdrawalRun>> action,
        bool created = false)
    {
        try
        {
            var result = await action().ConfigureAwait(false);
            return created ? StatusCode(StatusCodes.Status201Created, result) : Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(exception.Message); }
        catch (AdminWithdrawalExecutionDisabledException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, exception.Message);
        }
        catch (EconomyCapabilityAuthorizationException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                state = exception.State,
                diagnostics = exception.Diagnostics
            });
        }
        catch (ReserveInputUnknownException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, exception.Message);
        }
        catch (ReserveShortfallException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, exception.Message);
        }
        catch (AdminWithdrawalOverlapException exception) { return Conflict(exception.Message); }
        catch (AdminWithdrawalApprovalException exception) { return Conflict(exception.Message); }
        catch (AdminWithdrawalStaleCommandException exception) { return Conflict(exception.Message); }
        catch (AdminWithdrawalEvidenceException exception) { return Conflict(exception.Message); }
        catch (AdminWithdrawalEligibilityException exception) { return Conflict(exception.Message); }
        catch (GameGuild.Identity.Authentication.StepUpReceiptInvalidException exception)
        {
            return Conflict(exception.Message);
        }
    }

    private bool TryActor(out Guid tenantId, out Guid actorId)
    {
        var actor = actorContextAccessor.ActorContext;
        tenantId = actor.TenantId ?? Guid.Empty;
        actorId = actor.SubjectIdAsGuid ?? Guid.Empty;
        return actor.IsAuthenticated && tenantId != Guid.Empty && actorId != Guid.Empty &&
               actor.HasPermission(EconomyPermission.Keys.OperateTreasury);
    }
}
