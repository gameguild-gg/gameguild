namespace GameGuild.Commerce.Payments.Models;

/// <summary>
///     Request to update wallet settings
/// </summary>
public sealed record PatchWalletRequest(
    string? Currency = null,
    decimal? DailyLimit = null,
    decimal? MonthlyLimit = null);

/// <summary>
///     Request to freeze a wallet
/// </summary>
public sealed record FreezeWalletRequest(string Reason);

/// <summary>
///     Wallet audit log entry
/// </summary>
public record WalletAuditEntry(
    Guid Id,
    Guid WalletId,
    string Action,
    string? Details,
    decimal? Amount,
    decimal? BalanceAfter,
    DateTime Timestamp,
    string? PerformedBy);

/// <summary>
///     Paginated wallet audit log response
/// </summary>
public sealed record WalletAuditLogResponse(
    IReadOnlyList<WalletAuditEntry> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

/// <summary>
///     Paginated list of wallets response
/// </summary>
public sealed record WalletListResponse(
    IReadOnlyList<WalletSummary> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

/// <summary>
///     Summary view of a wallet for list responses
/// </summary>
public record WalletSummary(
    Guid Id,
    Guid UserId,
    string Currency,
    decimal Balance,
    bool IsFrozen,
    DateTime CreatedAt,
    DateTime? LastTransactionAt);
