using GameGuild.CQRS;
using GameGuild.Modules.Payments.Commands;
using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.Modules.Payments.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Payments.Controllers;

/// <summary>
///     Wallet management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class WalletController : ControllerBase
{
    private readonly ISender _sender;

    public WalletController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    ///     Create a new wallet for a user
    /// </summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(UserWallet), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWallet([FromBody] CreateWalletRequest request, CancellationToken ct)
    {
        var command = new CreateWalletCommand
        {
            UserId = request.UserId,
            Currency = request.Currency ?? "USD"
        };

        var result = await _sender.Send(command, ct);
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
        var query = new GetWalletQuery { UserId = userId };
        var result = await _sender.Send(query, ct);

        if (result == null)
            return NotFound($"Wallet not found for user {userId}");

        return Ok(result);
    }

    /// <summary>
    ///     Get wallet balance for a user
    /// </summary>
    [HttpGet("{userId:guid}/balance")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(Guid userId, CancellationToken ct)
    {
        var query = new GetWalletBalanceQuery { UserId = userId };
        var result = await _sender.Send(query, ct);
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
        var command = new AddFundsCommand
        {
            UserId = request.UserId,
            Amount = request.Amount,
            Description = request.Description,
            ReferenceId = request.ReferenceId
        };

        var result = await _sender.Send(command, ct);
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
        var command = new DeductFundsCommand
        {
            UserId = request.UserId,
            Amount = request.Amount,
            Description = request.Description,
            ReferenceId = request.ReferenceId
        };

        var result = await _sender.Send(command, ct);
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
        var command = new TransferFundsCommand
        {
            FromUserId = request.FromUserId,
            ToUserId = request.ToUserId,
            Amount = request.Amount,
            Description = request.Description,
            ReferenceId = request.ReferenceId
        };

        var (debitTransaction, creditTransaction) = await _sender.Send(command, ct);

        return Ok(new TransferFundsResponse
        {
            DebitTransaction = debitTransaction,
            CreditTransaction = creditTransaction
        });
    }

    /// <summary>
    ///     Lock a wallet
    /// </summary>
    [HttpPost("{userId:guid}/lock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LockWallet(Guid userId, [FromBody] LockWalletRequest request, CancellationToken ct)
    {
        var command = new LockWalletCommand
        {
            UserId = userId,
            Reason = request.Reason
        };

        await _sender.Send(command, ct);
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
        var command = new UnlockWalletCommand { UserId = userId };
        await _sender.Send(command, ct);
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
        [FromQuery] WalletTransactionType? typeFilter = null,
        [FromQuery] TransactionStatus? statusFilter = null,
        CancellationToken ct = default)
    {
        var query = new GetTransactionHistoryQuery
        {
            UserId = userId,
            Skip = skip,
            Take = take,
            TypeFilter = typeFilter,
            StatusFilter = statusFilter
        };

        var result = await _sender.Send(query, ct);
        return Ok(result);
    }
}

// DTOs
public record CreateWalletRequest
{
    public required Guid UserId { get; init; }
    public string? Currency { get; init; }
}

public record AddFundsRequest
{
    public required Guid UserId { get; init; }
    public required decimal Amount { get; init; }
    public required string Description { get; init; }
    public string? ReferenceId { get; init; }
}

public record DeductFundsRequest
{
    public required Guid UserId { get; init; }
    public required decimal Amount { get; init; }
    public required string Description { get; init; }
    public string? ReferenceId { get; init; }
}

public record TransferFundsRequest
{
    public required Guid FromUserId { get; init; }
    public required Guid ToUserId { get; init; }
    public required decimal Amount { get; init; }
    public required string Description { get; init; }
    public string? ReferenceId { get; init; }
}

public record TransferFundsResponse
{
    public required WalletTransaction DebitTransaction { get; init; }
    public required WalletTransaction CreditTransaction { get; init; }
}

public record LockWalletRequest
{
    public required string Reason { get; init; }
}
