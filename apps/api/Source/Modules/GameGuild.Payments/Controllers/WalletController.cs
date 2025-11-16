using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Payments.Commands;
using GameGuild.Payments.Entities;
using GameGuild.Payments.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Payments.Controllers;

/// <summary>
///     Wallet management controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[AllowAnonymous]
public sealed class WalletController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Create a new wallet for a user
    /// </summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(UserWallet), StatusCodes.Status200OK)]
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

        return Ok(result);
    }

    /// <summary>
    ///     Get wallet by user ID
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserWallet), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWallet(Guid userId, CancellationToken ct)
    {
        var query = new GetWalletByUserIdQuery(userId);
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        if (result == null) return NotFound($"Wallet not found for user {userId}");

        return Ok(result);
    }

    /// <summary>
    ///     Get wallet balance for a user
    /// </summary>
    [HttpGet("{userId:guid}/balance")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(Guid userId, CancellationToken ct)
    {
        var query = new GetWalletBalanceQuery(userId);
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Add funds to a wallet
    /// </summary>
    [HttpPost("add-funds")]
    [ProducesResponseType(typeof(WalletTransaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddFunds([FromBody] AddFundsRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new AddFundsCommand(request.UserId, request.Amount, request.Description, request.ReferenceId);

        var result = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Deduct funds from a wallet
    /// </summary>
    [HttpPost("deduct-funds")]
    [ProducesResponseType(typeof(WalletTransaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeductFunds([FromBody] DeductFundsRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new DeductFundsCommand(request.UserId, request.Amount, request.Description, request.ReferenceId);

        var result = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Transfer funds between wallets
    /// </summary>
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(TransferFundsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TransferFunds([FromBody] TransferFundsRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new TransferFundsCommand(request.FromUserId, request.ToUserId, request.Amount, request.Description, request.ReferenceId);

        (var debitTransaction, var creditTransaction) = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(new TransferFundsResponse { DebitTransaction = debitTransaction, CreditTransaction = creditTransaction });
    }

    /// <summary>
    ///     Lock a wallet
    /// </summary>
    [HttpPost("{userId:guid}/lock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LockWallet(Guid userId, [FromBody] LockWalletRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new LockWalletCommand(userId, request.Reason);
        await sender.Send(command, ct).ConfigureAwait(false);

        return Ok();
    }

    /// <summary>
    ///     Unlock a wallet
    /// </summary>
    [HttpPost("{userId:guid}/unlock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnlockWallet(Guid userId, CancellationToken ct)
    {
        var command = new UnlockWalletCommand(userId);
        await sender.Send(command, ct).ConfigureAwait(false);

        return Ok();
    }

    /// <summary>
    ///     Get transaction history for a wallet
    /// </summary>
    [HttpGet("{userId:guid}/transactions")]
    [ProducesResponseType(typeof(List<WalletTransaction>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionHistory(
        Guid userId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        WalletTransactionType? typeFilter = null,
        TransactionStatus? statusFilter = null,
        CancellationToken ct = default
    )
    {
        var query = new GetTransactionHistoryQuery(userId, skip, take, typeFilter, statusFilter);

        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(result);
    }
}

// DTOs
