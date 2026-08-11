using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Economy.Commands;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Funding;
using GameGuild.Economy.Queries;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/economy")]
[Tags("economy")]
[Authorize]
public sealed class EconomyWalletController(ISender sender, IActorContextAccessor actorContextAccessor) : BaseApiController
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
    private bool HasSelfServiceContext()
    {
        var actor = actorContextAccessor.ActorContext;
        return actor.IsAuthenticated && actor.SubjectIdAsGuid.HasValue && actor.TenantId.HasValue;
    }
}
