using Asp.Versioning;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Marketplace;
using GameGuild.Economy.Risk;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Controllers;

public sealed record SettleMyMarketplaceOrderRequest(
    MarketplaceCurrencyChoice CurrencyChoice,
    string JurisdictionCode,
    Guid RiskDecisionId,
    string OperationFingerprint,
    string IdempotencyKey);

public sealed record RefundMarketplaceSettlementRequest(
    int Quantity,
    string ReasonCode,
    string JurisdictionCode,
    Guid RiskDecisionId,
    string OperationFingerprint,
    string IdempotencyKey);

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/economy/marketplace")]
[Tags("economy-marketplace")]
[Authorize]
public sealed class EconomyMarketplaceController(
    IDurableMarketplaceSettlementService settlements,
    IDurableMarketplaceRefundService refunds,
    IActorContextAccessor actorContextAccessor,
    TimeProvider timeProvider) : BaseApiController
{
    [HttpPost("orders/{orderId:guid}:settle")]
    [ProducesResponseType(typeof(DurableMarketplaceSettlementResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Settle(
        Guid orderId,
        [FromBody] SettleMyMarketplaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out var actorId)) return Forbid();
        ArgumentNullException.ThrowIfNull(request);
        return Ok(await settlements.SettleAsync(new SettleAuthoritativeMarketplaceOrderRequest(
            tenantId,
            actorId,
            orderId,
            request.CurrencyChoice,
            EconomySubjectReference.ForUser(tenantId, actorId),
            request.JurisdictionCode,
            request.RiskDecisionId,
            request.OperationFingerprint,
            new IdempotencyKey(request.IdempotencyKey),
            timeProvider.GetUtcNow()), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("settlements/{settlementId:guid}:refund")]
    [ProducesResponseType(typeof(DurableMarketplaceRefundResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refund(
        Guid settlementId,
        [FromBody] RefundMarketplaceSettlementRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var tenantId, out var actorId)) return Forbid();
        ArgumentNullException.ThrowIfNull(request);
        return Ok(await refunds.RefundAsync(new RefundAuthoritativeMarketplaceOrderRequest(
            tenantId,
            actorId,
            MarketplaceRefundAuthority.SelfService,
            settlementId,
            request.Quantity,
            request.ReasonCode,
            EconomySubjectReference.ForUser(tenantId, actorId),
            request.JurisdictionCode,
            request.RiskDecisionId,
            request.OperationFingerprint,
            new IdempotencyKey(request.IdempotencyKey),
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
[Route("api/v{version:apiVersion}/admin/economy/marketplace")]
[Tags("economy-administration")]
[Authorize]
public sealed class EconomyMarketplaceAdministrationController(
    IDurableMarketplaceRefundService refunds,
    IActorContextAccessor actorContextAccessor,
    TimeProvider timeProvider) : BaseApiController
{
    [HttpPost("settlements/{settlementId:guid}:refund")]
    [ProducesResponseType(typeof(DurableMarketplaceRefundResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refund(
        Guid settlementId,
        [FromBody] RefundMarketplaceSettlementRequest request,
        CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || !actor.TenantId.HasValue || !actor.SubjectIdAsGuid.HasValue ||
            !actor.HasPermission(EconomyPermission.Keys.OperateMarketplace))
            return Forbid();
        ArgumentNullException.ThrowIfNull(request);
        var actorId = actor.SubjectIdAsGuid.Value;
        return Ok(await refunds.RefundAsync(new RefundAuthoritativeMarketplaceOrderRequest(
            actor.TenantId.Value,
            actorId,
            MarketplaceRefundAuthority.Operations,
            settlementId,
            request.Quantity,
            request.ReasonCode,
            EconomySubjectReference.ForUser(actor.TenantId.Value, actorId),
            request.JurisdictionCode,
            request.RiskDecisionId,
            request.OperationFingerprint,
            new IdempotencyKey(request.IdempotencyKey),
            timeProvider.GetUtcNow()), cancellationToken).ConfigureAwait(false));
    }
}
