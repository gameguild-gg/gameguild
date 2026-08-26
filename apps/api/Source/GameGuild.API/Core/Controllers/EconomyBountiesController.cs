using Asp.Versioning;
using GameGuild.Economy.Bounties;
using GameGuild.Economy.Contracts;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Controllers;

public sealed record CreateMyBountyRequest(
    CurrencyCode Currency,
    long AmountUnits,
    bool RequiresPrerequisite,
    int MinimumReputation,
    bool RequiresInstructorVerification,
    DateTimeOffset ExpiresAt,
    string JurisdictionCode,
    Guid RiskDecisionId,
    string OperationFingerprint,
    string IdempotencyKey);

public sealed record CompleteMyBountyRequest(
    string JurisdictionCode,
    Guid RiskDecisionId,
    string OperationFingerprint,
    string IdempotencyKey);

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/economy/bounties")]
[Tags("economy-bounties")]
[Authorize]
public sealed class EconomyBountiesController(
    IDurableBountyApplicationService bounties,
    IActorContextAccessor actorContextAccessor,
    TimeProvider timeProvider) : BaseApiController
{
    [HttpPost]
    [ProducesResponseType(typeof(DurableBountyView), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMyBountyRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out var actorId)) return Forbid();
        ArgumentNullException.ThrowIfNull(request);
        var result = await bounties.CreateAsync(new CreateDurableBountyRequest(
            tenantId,
            actorId,
            new CoinAmount(request.Currency, request.AmountUnits),
            new BountyEligibilityRequirements(
                request.RequiresPrerequisite,
                request.MinimumReputation,
                request.RequiresInstructorVerification),
            request.ExpiresAt,
            request.JurisdictionCode,
            request.RiskDecisionId,
            request.OperationFingerprint,
            new IdempotencyKey(request.IdempotencyKey),
            timeProvider.GetUtcNow()), cancellationToken);
        return CreatedAtAction(nameof(Get), new { version = "1", bountyId = result.Id.Value }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DurableBountyView>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] BountyStatus? status,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out _)) return Forbid();
        return Ok(await bounties.ListAsync(tenantId, status, cancellationToken));
    }

    [HttpGet("{bountyId:guid}")]
    [ProducesResponseType(typeof(DurableBountyView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid bountyId, CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out _)) return Forbid();
        var result = await bounties.FindAsync(tenantId, new BountyId(bountyId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{bountyId:guid}:claim")]
    [ProducesResponseType(typeof(DurableBountyView), StatusCodes.Status200OK)]
    public async Task<IActionResult> Claim(
        Guid bountyId,
        [FromBody] CompleteMyBountyRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out var actorId)) return Forbid();
        ArgumentNullException.ThrowIfNull(request);
        return Ok(await bounties.ClaimAsync(new ClaimDurableBountyRequest(
            tenantId,
            actorId,
            new BountyId(bountyId),
            request.JurisdictionCode,
            request.RiskDecisionId,
            request.OperationFingerprint,
            new IdempotencyKey(request.IdempotencyKey),
            timeProvider.GetUtcNow()), cancellationToken));
    }

    [HttpPost("{bountyId:guid}:reclaim")]
    [ProducesResponseType(typeof(DurableBountyView), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reclaim(
        Guid bountyId,
        [FromBody] CompleteMyBountyRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out var actorId)) return Forbid();
        ArgumentNullException.ThrowIfNull(request);
        return Ok(await bounties.ReclaimAsync(new ReclaimDurableBountyRequest(
            tenantId,
            actorId,
            new BountyId(bountyId),
            request.JurisdictionCode,
            request.RiskDecisionId,
            request.OperationFingerprint,
            new IdempotencyKey(request.IdempotencyKey),
            timeProvider.GetUtcNow()), cancellationToken));
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
[Route("api/v{version:apiVersion}/admin/economy/bounties")]
[Tags("economy-administration")]
[Authorize]
public sealed class EconomyBountiesAdministrationController(
    IDurableBountyApplicationService bounties,
    IActorContextAccessor actorContextAccessor) : BaseApiController
{
    [HttpGet("expired")]
    [ProducesResponseType(typeof(IReadOnlyList<DurableBountyView>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListExpired(CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || !actor.TenantId.HasValue ||
            !actor.HasPermission(EconomyPermission.Keys.OperateBounties))
            return Forbid();
        return Ok(await bounties.ListAsync(
            actor.TenantId.Value, BountyStatus.Expired, cancellationToken));
    }
}
