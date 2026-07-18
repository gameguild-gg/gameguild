using GameGuild.Commerce.Payments.Models;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments.Queries.ListWallets;

/// <summary>
///     Query to list all wallets with pagination and filtering
/// </summary>
[AuthorizeRequest(WalletsPermission.Keys.Admin)]
public sealed record ListWalletsQuery(
    int Page,
    int PageSize,
    string? Currency = null,
    bool? IsFrozen = null) : IQuery<WalletListResponse>;
