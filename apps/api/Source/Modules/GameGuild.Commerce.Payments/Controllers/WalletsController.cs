using Asp.Versioning;
using GameGuild.CQRS;



using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Wallets management controller - RESTful API following Google API Design Guidelines
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Tags("wallets")]
[AllowAnonymous]
public sealed class WalletsController(ISender sender) : ControllerBase
{
    #region Collection Operations - /v1/wallets

    /// <summary>
    ///     Create a new wallet for a user
    /// </summary>
    [HttpPost("v{version:apiVersion}/wallets")]
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

        return CreatedAtAction(nameof(GetWalletById), new { walletId = result.Id }, result);
    }

    #endregion

    #region Individual Wallet Operations - /v1/wallets/{walletId}

    /// <summary>
    ///     Get wallet by ID
    /// </summary>
    [HttpGet("v{version:apiVersion}/wallets/{walletId:guid}")]
    [EndpointSummary("Get wallet by ID")]
    [EndpointDescription("Retrieves wallet details by wallet ID.")]
    [ProducesResponseType(typeof(UserWallet), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWalletById(Guid walletId, CancellationToken ct)
    {
        var query = new GetWalletByIdQuery(walletId);
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        if (result == null) return NotFound($"Wallet not found: {walletId}");

        return Ok(result);
    }

    /// <summary>
    ///     Get wallet balance
    /// </summary>
    [HttpGet("v{version:apiVersion}/wallets/{walletId:guid}/balance")]
    [EndpointSummary("Get wallet balance")]
    [EndpointDescription("Retrieves the current balance of a wallet.")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance(Guid walletId, CancellationToken ct)
    {
        var query = new GetWalletBalanceByIdQuery(walletId);
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Get transaction history for a wallet
    /// </summary>
    [HttpGet("v{version:apiVersion}/wallets/{walletId:guid}/transactions")]
    [EndpointSummary("Get wallet transactions")]
    [EndpointDescription("Retrieves transaction history for a wallet.")]
    [ProducesResponseType(typeof(List<WalletTransaction>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionHistory(
        Guid walletId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] WalletTransactionType? typeFilter = null,
        [FromQuery] TransactionStatus? statusFilter = null,
        CancellationToken ct = default
    )
    {
        var query = new GetWalletTransactionHistoryQuery(walletId, skip, take, typeFilter, statusFilter);

        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(result);
    }

    #endregion

    #region Wallet Actions - /v1/wallets/{walletId}:action

    /// <summary>
    ///     Add funds to a wallet
    /// </summary>
    [HttpPost("v{version:apiVersion}/wallets/{walletId:guid}:add-funds")]
    [EndpointSummary("Add funds to wallet")]
    [EndpointDescription("Adds funds to the specified wallet.")]
    [ProducesResponseType(typeof(WalletTransaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFunds(Guid walletId, [FromBody] AddFundsRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new AddFundsToWalletCommand(walletId, request.Amount, request.Description, request.ReferenceId);

        var result = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Deduct funds from a wallet
    /// </summary>
    [HttpPost("v{version:apiVersion}/wallets/{walletId:guid}:deduct-funds")]
    [EndpointSummary("Deduct funds from wallet")]
    [EndpointDescription("Deducts funds from the specified wallet.")]
    [ProducesResponseType(typeof(WalletTransaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeductFunds(Guid walletId, [FromBody] DeductFundsRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new DeductFundsFromWalletCommand(walletId, request.Amount, request.Description, request.ReferenceId);

        var result = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Transfer funds to another wallet
    /// </summary>
    [HttpPost("v{version:apiVersion}/wallets/{walletId:guid}:transfer")]
    [EndpointSummary("Transfer funds between wallets")]
    [EndpointDescription("Transfers funds from this wallet to another wallet.")]
    [ProducesResponseType(typeof(TransferFundsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferFunds(Guid walletId, [FromBody] TransferFundsRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new TransferFundsBetweenWalletsCommand(walletId, request.ToWalletId, request.Amount, request.Description, request.ReferenceId);

        (var debitTransaction, var creditTransaction) = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(new TransferFundsResponse { DebitTransaction = debitTransaction, CreditTransaction = creditTransaction });
    }

    /// <summary>
    ///     Lock a wallet
    /// </summary>
    [HttpPost("v{version:apiVersion}/wallets/{walletId:guid}:lock")]
    [EndpointSummary("Lock wallet")]
    [EndpointDescription("Locks a wallet to prevent transactions.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LockWallet(Guid walletId, [FromBody] LockWalletRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new LockWalletByIdCommand(walletId, request.Reason);
        await sender.Send(command, ct).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Unlock a wallet
    /// </summary>
    [HttpPost("v{version:apiVersion}/wallets/{walletId:guid}:unlock")]
    [EndpointSummary("Unlock wallet")]
    [EndpointDescription("Unlocks a wallet to allow transactions.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlockWallet(Guid walletId, CancellationToken ct)
    {
        var command = new UnlockWalletByIdCommand(walletId);
        await sender.Send(command, ct).ConfigureAwait(false);

        return NoContent();
    }

    #endregion

    #region User Wallet Convenience - /v1/users/{userId}/wallet

    /// <summary>
    ///     Get wallet by user ID (convenience endpoint)
    /// </summary>
    [HttpGet("v{version:apiVersion}/users/{userId:guid}/wallet")]
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
    ///     Get wallet balance by user ID (convenience endpoint)
    /// </summary>
    [HttpGet("v{version:apiVersion}/users/{userId:guid}/wallet/balance")]
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

    #endregion
}

// DTOs
