using Asp.Versioning;
using GameGuild.Commerce.Payments.Commands.CloseWallet;
using GameGuild.Commerce.Payments.Commands.FreezeWallet;
using GameGuild.Commerce.Payments.Commands.PatchWallet;
using GameGuild.Commerce.Payments.Commands.UnfreezeWallet;
using GameGuild.Commerce.Payments.Models;
using GameGuild.Commerce.Payments.Queries.GetWalletAuditLog;
using GameGuild.Commerce.Payments.Queries.GetWalletById;
using GameGuild.Commerce.Payments.Queries.ListWallets;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Self-service wallet access and explicitly privileged platform administration.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
[Microsoft.AspNetCore.Http.Tags("wallets")]
[Authorize]
public sealed class WalletsController(ISender sender, IActorContextAccessor actorContextAccessor) : BaseApiController
{
    [HttpPost("wallet")]
    [EndpointSummary("Create my wallet")]
    [ProducesResponseType(typeof(UserWallet), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMyWallet([FromBody] CreateWalletRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!HasSelfServiceContext()) return Forbid();

        var wallet = await sender.Send(new CreateMyWalletCommand(request.Currency ?? "USD"), ct).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetMyWallet), wallet);
    }

    [HttpGet("wallet")]
    [EndpointSummary("Get my wallet")]
    [ProducesResponseType(typeof(UserWallet), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyWallet(CancellationToken ct)
    {
        if (!HasSelfServiceContext()) return Forbid();

        var wallet = await sender.Send(new GetMyWalletQuery(), ct).ConfigureAwait(false);
        return wallet is null ? NotFound() : Ok(wallet);
    }

    [HttpGet("wallet/balance")]
    [EndpointSummary("Get my wallet balance")]
    public async Task<IActionResult> GetMyWalletBalance(CancellationToken ct)
    {
        if (!HasSelfServiceContext()) return Forbid();

        return Ok(await sender.Send(new GetMyWalletBalanceQuery(), ct).ConfigureAwait(false));
    }

    [HttpPost("wallet:lock")]
    [EndpointSummary("Lock my wallet")]
    public async Task<IActionResult> LockMyWallet([FromBody] LockWalletRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!HasSelfServiceContext()) return Forbid();

        await sender.Send(new LockMyWalletCommand(request.Reason), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("wallet:unlock")]
    [EndpointSummary("Unlock my wallet")]
    public async Task<IActionResult> UnlockMyWallet(CancellationToken ct)
    {
        if (!HasSelfServiceContext()) return Forbid();

        await sender.Send(new UnlockMyWalletCommand(), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("wallets")]
    [EndpointSummary("List all wallets")]
    public async Task<IActionResult> ListWallets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? currency = null,
        [FromQuery] bool? isFrozen = null,
        CancellationToken ct = default)
    {
        if (!CanAdministerWallets()) return Forbid();
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await sender.Send(new ListWalletsQuery(page, pageSize, currency, isFrozen), ct).ConfigureAwait(false));
    }

    [HttpGet("wallets/{walletId:guid}")]
    [EndpointSummary("Get wallet by ID")]
    public async Task<IActionResult> GetWalletById(Guid walletId, CancellationToken ct)
    {
        if (!CanAdministerWallets()) return Forbid();
        var wallet = await sender.Send(new GetWalletByIdQuery(walletId), ct).ConfigureAwait(false);
        return wallet is null ? NotFound() : Ok(wallet);
    }

    [HttpHead("wallets/{walletId:guid}")]
    [EndpointSummary("Check if wallet exists")]
    public async Task<IActionResult> CheckWalletExists(Guid walletId, CancellationToken ct)
    {
        if (!CanAdministerWallets()) return Forbid();
        return await sender.Send(new GetWalletByIdQuery(walletId), ct).ConfigureAwait(false) is null ? NotFound() : Ok();
    }

    [HttpPatch("wallets/{walletId:guid}")]
    [EndpointSummary("Update wallet settings")]
    public async Task<IActionResult> PatchWallet(Guid walletId, [FromBody] PatchWalletRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (!CanAdministerWallets()) return Forbid();
        await sender.Send(new PatchWalletCommand(walletId, body.Currency, body.DailyLimit, body.MonthlyLimit), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("wallets/{walletId:guid}")]
    [EndpointSummary("Close wallet")]
    public async Task<IActionResult> DeleteWallet(Guid walletId, CancellationToken ct)
    {
        if (!CanAdministerWallets()) return Forbid();
        await sender.Send(new CloseWalletCommand(walletId), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("wallets/{walletId:guid}:freeze")]
    [EndpointSummary("Freeze wallet")]
    public async Task<IActionResult> FreezeWallet(Guid walletId, [FromBody] FreezeWalletRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (!CanAdministerWallets()) return Forbid();
        await sender.Send(new FreezeWalletCommand(walletId, body.Reason), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("wallets/{walletId:guid}:unfreeze")]
    [EndpointSummary("Unfreeze wallet")]
    public async Task<IActionResult> UnfreezeWallet(Guid walletId, CancellationToken ct)
    {
        if (!CanAdministerWallets()) return Forbid();
        await sender.Send(new UnfreezeWalletCommand(walletId), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("wallets/{walletId:guid}/audit-log")]
    [EndpointSummary("Get wallet audit log")]
    public async Task<IActionResult> GetWalletAuditLog(
        Guid walletId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!CanAdministerWallets()) return Forbid();
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await sender.Send(new GetWalletAuditLogQuery(walletId, page, pageSize), ct).ConfigureAwait(false));
    }

    private bool HasSelfServiceContext()
    {
        var actor = actorContextAccessor.ActorContext;
        return actor.IsAuthenticated && actor.SubjectIdAsGuid.HasValue && actor.TenantId.HasValue;
    }

    private bool CanAdministerWallets()
    {
        var actor = actorContextAccessor.ActorContext;
        return actor.IsAuthenticated && actor.TenantId.HasValue &&
               actor.HasPermission(WalletsPermission.Keys.Admin);
    }
}
