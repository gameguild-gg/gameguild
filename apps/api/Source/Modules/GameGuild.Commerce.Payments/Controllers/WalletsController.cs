using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Wallets management controller - RESTful API following Google API Design Guidelines.
///     Note: Wallet operations are currently user-based. Wallet-ID-based operations 
///     will be implemented in a future iteration.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}")]
[Tags("wallets")]
[Authorize]
public sealed class WalletsController(ISender sender) : ControllerBase
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
}

// DTOs
