using Asp.Versioning;
using GameGuild.API.Setup;
using GameGuild.CQRS;
using GameGuild.Economy.Commands;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Funding;
using GameGuild.Economy.Payouts.Queries;
using GameGuild.Economy.Queries;
using GameGuild.Economy.Risk;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Controllers;

public sealed record EconomySelfServiceCapabilityDto(
    EconomyValueMovementCapability Capability,
    EconomyCapabilityReadinessState State,
    IReadOnlyList<string> Diagnostics);

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/economy")]
[Tags("economy")]
[Authorize]
public sealed class EconomyWalletController(
    ISender sender,
    IActorContextAccessor actorContextAccessor,
    IEconomyProviderCapabilityReadiness capabilityReadiness) : BaseApiController
{
    [HttpGet("wallet")]
    [EndpointSummary("Get my Economy wallet")]
    [ProducesResponseType(typeof(EconomyWalletSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyWallet(CancellationToken cancellationToken)
    {
        if (!HasSelfServiceContext())
            return Forbid();

        var wallet = await sender.Send(new GetMyEconomyWalletQuery(), cancellationToken).ConfigureAwait(false);
        return wallet is null ? NotFound() : Ok(wallet);
    }

    [HttpGet("wallet/transactions")]
    [EndpointSummary("List my Economy wallet transactions")]
    [ProducesResponseType(typeof(IReadOnlyList<EconomyWalletTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyTransactions([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        if (!HasSelfServiceContext())
            return Forbid();

        return Ok(await sender.Send(new ListMyEconomyWalletTransactionsQuery(take), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("conversions/hard-to-soft")]
    [EndpointSummary("Convert my confirmed HardCoin balance into SoftCoin")]
    [ProducesResponseType(typeof(SelfServiceHardToSoftConversionReceipt), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ConvertMyHardToSoft(
        [FromBody] ConvertMyHardToSoftRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasSelfServiceContext())
            return Forbid();

        ArgumentNullException.ThrowIfNull(request);
        var receipt = await sender.Send(new ConvertMyHardToSoftCommand(request), cancellationToken).ConfigureAwait(false);
        return Ok(receipt);
    }

    [HttpGet("capabilities")]
    [EndpointSummary("Get my Economy capability readiness")]
    [ProducesResponseType(typeof(IReadOnlyList<EconomySelfServiceCapabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetMyCapabilityReadiness()
    {
        if (!HasSelfServiceContext())
            return Forbid();

        EconomyValueMovementCapability[] capabilities =
        [
            EconomyValueMovementCapability.ConvertHardToSoft,
            EconomyValueMovementCapability.PayoutExecution
        ];
        var result = capabilities
            .Select(capability =>
            {
                var readiness = capabilityReadiness.Assess(capability);
                return new EconomySelfServiceCapabilityDto(
                    capability,
                    readiness.State,
                    [.. readiness.Diagnostics]);
            })
            .ToArray();
        return Ok(result);
    }

    [HttpGet("payouts")]
    [EndpointSummary("List my payout operations")]
    [ProducesResponseType(typeof(IReadOnlyList<EconomyPayoutOperationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListMyPayouts(
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetSelfServiceActorId(out var actorId))
            return Forbid();
        if (take is < 1 or > 100)
            return BadRequest("Take must be between 1 and 100.");

        var payouts = await sender.Send(new ListMyPayoutOperationsQuery(actorId, take), cancellationToken)
            .ConfigureAwait(false);
        return Ok(payouts);
    }

    [HttpGet("payouts/{operationId:guid}")]
    [EndpointSummary("Get my payout operation")]
    [ProducesResponseType(typeof(EconomyPayoutOperationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyPayout(
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetSelfServiceActorId(out var actorId))
            return Forbid();

        var payout = await sender.Send(new GetMyPayoutOperationQuery(actorId, operationId), cancellationToken)
            .ConfigureAwait(false);
        return payout is null ? NotFound() : Ok(payout);
    }

    private bool HasSelfServiceContext()
    {
        var actor = actorContextAccessor.ActorContext;
        return actor.IsAuthenticated && actor.SubjectIdAsGuid.HasValue && actor.TenantId.HasValue;
    }

    private bool TryGetSelfServiceActorId(out Guid actorId)
    {
        actorId = Guid.Empty;
        if (!HasSelfServiceContext())
            return false;

        actorId = actorContextAccessor.ActorContext.SubjectIdAsGuid!.Value;
        return true;
    }
}
