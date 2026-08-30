using Asp.Versioning;
using GameGuild.API.Authorization;
using GameGuild.Economy.Risk;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/economy/compliance/holds")]
[Tags("economy-compliance-hold-administration")]
[Authorize]
public sealed class EconomyComplianceHoldAdministrationController(
    IComplianceHoldAdministrationStore holds,
    IEconomyStepUpExecutor stepUp,
    IActorContextAccessor actorContextAccessor,
    TimeProvider timeProvider) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(ComplianceHoldPage), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] bool? active = true,
        [FromQuery] EconomyValueMovementCapability? capability = null,
        [FromQuery] int limit = 50,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryOperator(out var tenantId, out _)) return Forbid();
        return Ok(await holds.ListAsync(
            tenantId,
            active,
            capability,
            limit,
            cursor,
            timeProvider.GetUtcNow(),
            cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("{holdId:guid}")]
    [ProducesResponseType(typeof(ComplianceHoldAdministrationState), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid holdId, CancellationToken cancellationToken)
    {
        if (!TryOperator(out var tenantId, out _)) return Forbid();
        try
        {
            return Ok(await holds.CurrentAsync(tenantId, holdId, cancellationToken)
                .ConfigureAwait(false));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{holdId:guid}/audit")]
    [ProducesResponseType(typeof(IReadOnlyList<ComplianceHoldEvent>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Audit(Guid holdId, CancellationToken cancellationToken)
    {
        if (!TryOperator(out var tenantId, out _)) return Forbid();
        try
        {
            return Ok(await holds.EventsAsync(tenantId, holdId, cancellationToken)
                .ConfigureAwait(false));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{holdId:guid}/release-proposals")]
    [ProducesResponseType(typeof(ComplianceHoldAdministrationState), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> ProposeRelease(
        Guid holdId,
        [FromBody] EconomyStepUpRequest request,
        CancellationToken cancellationToken) =>
        MutateRelease(holdId, request, approve: false, cancellationToken);

    [HttpPost("{holdId:guid}/release-approvals")]
    [ProducesResponseType(typeof(ComplianceHoldAdministrationState), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> ApproveRelease(
        Guid holdId,
        [FromBody] EconomyStepUpRequest request,
        CancellationToken cancellationToken) =>
        MutateRelease(holdId, request, approve: true, cancellationToken);

    private async Task<IActionResult> MutateRelease(
        Guid holdId,
        EconomyStepUpRequest request,
        bool approve,
        CancellationToken cancellationToken)
    {
        if (!TryOperator(out var tenantId, out var actorId)) return Forbid();
        ArgumentNullException.ThrowIfNull(request);
        var action = approve ? "approve" : "propose";
        var operation = EconomyStepUpOperation.Create(
            $"economy.compliance-hold.release.{action}",
            $"compliance-hold:{holdId:N}",
            holdId.ToString("N"));
        try
        {
            var result = await stepUp.ExecuteAsync(
                operation,
                request.StepUpReceipt,
                (evidenceHash, token) => approve
                    ? holds.ApproveReleaseAsync(
                        tenantId,
                        holdId,
                        actorId,
                        evidenceHash,
                        timeProvider.GetUtcNow(),
                        token).AsTask()
                    : holds.ProposeReleaseAsync(
                        tenantId,
                        holdId,
                        actorId,
                        evidenceHash,
                        timeProvider.GetUtcNow(),
                        token).AsTask(),
                cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }

    private bool TryOperator(out Guid tenantId, out Guid actorId)
    {
        tenantId = Guid.Empty;
        actorId = Guid.Empty;
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated ||
            actor.TenantId is not { } resolvedTenant ||
            actor.SubjectIdAsGuid is not { } resolvedActor ||
            !actor.HasPermission(EconomyPermission.Keys.OperateCompliance))
            return false;
        tenantId = resolvedTenant;
        actorId = resolvedActor;
        return true;
    }
}
