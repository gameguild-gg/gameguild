using GameGuild.Commerce.Payments.Models;
using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Queries.ListWallets;

/// <summary>
///     Query to list all wallets with pagination and filtering
/// </summary>
public record ListWalletsQuery(
    int Page,
    int PageSize,
    string? Currency = null,
    bool? IsFrozen = null) : IQuery<WalletListResponse>;
