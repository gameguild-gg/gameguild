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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Wallets management controller - RESTful API following Google API Design Guidelines.
///     Supports both user-based operations (/users/{userId}/wallet) and 
///     wallet-ID-based operations (/wallets/{walletId}).
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
[Tags("wallets")]
[Authorize]
public sealed class WalletsController(ISender sender) : BaseApiController
{
    #region Wallet Creation

    /// <summary>
    ///     Create a new wallet for a user
    /// </summary>
    [HttpPost("wallets")]
    [EndpointSummary("Create a new wallet")]
    [EndpointDescription("Creates a new wallet for the specified user.")]
    [ProducesResponseType(typeof(UserWallet), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWallet([FromBody] CreateWalletRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UserId == Guid.Empty)
        {
            return BadRequest(new { error = "UserId cannot be empty" });
        }

        var command = new CreateWalletCommand(request.UserId, request.Currency ?? "USD");
        var result = await sender.Send(command, ct).ConfigureAwait(false);

        return CreatedAtAction(nameof(GetWalletByUserId), new { userId = request.UserId }, result);
    }

    #endregion

    #region User Wallet Operations - /v1/users/{userId}/wallet

    /// <summary>
    ///     Get wallet by user ID
    /// </summary>
    [HttpGet("users/{userId:guid}/wallet")]
    [EndpointSummary("Get user's wallet")]
    [EndpointDescription("Retrieves the wallet for a specific user.")]
    [ProducesResponseType(typeof(UserWallet), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWalletByUserId(Guid userId, CancellationToken ct)
    {
        var query = new GetWalletByUserIdQuery(userId);
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        if (result == null) return NotFound($"Wallet not found for user {userId}");

        return Ok(result);
    }

    /// <summary>
    ///     Get wallet balance by user ID
    /// </summary>
    [HttpGet("users/{userId:guid}/wallet/balance")]
    [EndpointSummary("Get user's wallet balance")]
    [EndpointDescription("Retrieves the wallet balance for a specific user.")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalanceByUserId(Guid userId, CancellationToken ct)
    {
        var query = new GetWalletBalanceQuery(userId);
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Add funds to a user's wallet
    /// </summary>
    [HttpPost("users/{userId:guid}/wallet:add-funds")]
    [EndpointSummary("Add funds to user's wallet")]
    [EndpointDescription("Adds funds to the wallet for the specified user.")]
    [ProducesResponseType(typeof(WalletTransaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFunds(Guid userId, [FromBody] AddFundsRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new AddFundsCommand(userId, request.Amount, request.Description, request.ReferenceId);
        var result = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Deduct funds from a user's wallet
    /// </summary>
    [HttpPost("users/{userId:guid}/wallet:deduct-funds")]
    [EndpointSummary("Deduct funds from user's wallet")]
    [EndpointDescription("Deducts funds from the wallet for the specified user.")]
    [ProducesResponseType(typeof(WalletTransaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeductFunds(Guid userId, [FromBody] DeductFundsRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new DeductFundsCommand(userId, request.Amount, request.Description, request.ReferenceId);
        var result = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Transfer funds between user wallets
    /// </summary>
    [HttpPost("users/{userId:guid}/wallet:transfer")]
    [EndpointSummary("Transfer funds to another user's wallet")]
    [EndpointDescription("Transfers funds from this user's wallet to another user's wallet.")]
    [ProducesResponseType(typeof(TransferResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferFunds(Guid userId, [FromBody] TransferFundsRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new TransferFundsCommand(userId, request.ToUserId, request.Amount, request.Description, request.ReferenceId);
        var result = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Lock a user's wallet
    /// </summary>
    [HttpPost("users/{userId:guid}/wallet:lock")]
    [EndpointSummary("Lock user's wallet")]
    [EndpointDescription("Locks a user's wallet to prevent transactions.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LockWallet(Guid userId, [FromBody] LockWalletRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new LockWalletCommand(userId, request.Reason);
        await sender.Send(command, ct).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Unlock a user's wallet
    /// </summary>
    [HttpPost("users/{userId:guid}/wallet:unlock")]
    [EndpointSummary("Unlock user's wallet")]
    [EndpointDescription("Unlocks a user's wallet to allow transactions.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlockWallet(Guid userId, CancellationToken ct)
    {
        var command = new UnlockWalletCommand(userId);
        await sender.Send(command, ct).ConfigureAwait(false);

        return NoContent();
    }

    #endregion

    #region Wallet-ID-based Operations - /v1/wallets/{walletId}

    /// <summary>
    ///     List all wallets (admin only)
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="currency">Filter by currency</param>
    /// <param name="isFrozen">Filter by frozen status</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of wallets</returns>
    [HttpGet("wallets")]
    [Authorize(Policy = "RequireAdminRole")]
    [EndpointSummary("List all wallets")]
    [EndpointDescription("Retrieves a paginated list of all wallets. Admin only.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListWallets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? currency = null,
        [FromQuery] bool? isFrozen = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await sender.Send(new ListWalletsQuery(page, pageSize, currency, isFrozen), ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    ///     Get wallet by ID
    /// </summary>
    /// <param name="walletId">Wallet ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Wallet details</returns>
    [HttpGet("wallets/{walletId:guid}")]
    [EndpointSummary("Get wallet by ID")]
    [EndpointDescription("Retrieves detailed information for a specific wallet.")]
    [ProducesResponseType(typeof(UserWallet), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWalletById(Guid walletId, CancellationToken ct)
    {
        var result = await sender.Send(new GetWalletByIdQuery(walletId), ct).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    ///     Check if wallet exists by ID
    /// </summary>
    /// <param name="walletId">Wallet ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>200 if exists, 404 if not</returns>
    [HttpHead("wallets/{walletId:guid}")]
    [EndpointSummary("Check if wallet exists")]
    [EndpointDescription("Checks if a wallet exists without returning the body.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckWalletExists(Guid walletId, CancellationToken ct)
    {
        var result = await sender.Send(new GetWalletByIdQuery(walletId), ct).ConfigureAwait(false);
        return result is null ? NotFound() : Ok();
    }

    /// <summary>
    ///     Partially update wallet settings
    /// </summary>
    /// <param name="walletId">Wallet ID</param>
    /// <param name="body">Patch request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPatch("wallets/{walletId:guid}")]
    [EndpointSummary("Update wallet settings")]
    [EndpointDescription("Updates specific settings of a wallet.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PatchWallet(Guid walletId, [FromBody] PatchWalletRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new PatchWalletCommand(walletId, body.Currency, body.DailyLimit, body.MonthlyLimit), ct).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    ///     Close/delete a wallet
    /// </summary>
    /// <param name="walletId">Wallet ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("wallets/{walletId:guid}")]
    [Authorize(Policy = "RequireAdminRole")]
    [EndpointSummary("Close wallet")]
    [EndpointDescription("Closes a wallet. Requires zero balance and admin permissions.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteWallet(Guid walletId, CancellationToken ct)
    {
        await sender.Send(new CloseWalletCommand(walletId), ct).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    ///     Freeze a wallet
    /// </summary>
    /// <param name="walletId">Wallet ID</param>
    /// <param name="body">Freeze request with reason</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("wallets/{walletId:guid}:freeze")]
    [Authorize(Policy = "RequireAdminRole")]
    [EndpointSummary("Freeze wallet")]
    [EndpointDescription("Freezes a wallet to prevent all transactions.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FreezeWallet(Guid walletId, [FromBody] FreezeWalletRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new FreezeWalletCommand(walletId, body.Reason), ct).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    ///     Unfreeze a wallet
    /// </summary>
    /// <param name="walletId">Wallet ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("wallets/{walletId:guid}:unfreeze")]
    [Authorize(Policy = "RequireAdminRole")]
    [EndpointSummary("Unfreeze wallet")]
    [EndpointDescription("Unfreezes a wallet to allow transactions.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnfreezeWallet(Guid walletId, CancellationToken ct)
    {
        await sender.Send(new UnfreezeWalletCommand(walletId), ct).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    ///     Get wallet audit log
    /// </summary>
    /// <param name="walletId">Wallet ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated audit log entries</returns>
    [HttpGet("wallets/{walletId:guid}/audit-log")]
    [EndpointSummary("Get wallet audit log")]
    [EndpointDescription("Retrieves the audit log of all transactions and actions on a wallet.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWalletAuditLog(
        Guid walletId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await sender.Send(new GetWalletAuditLogQuery(walletId, page, pageSize), ct).ConfigureAwait(false);
        return Ok(result);
    }

    #endregion
}