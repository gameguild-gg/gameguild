using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Queries.GetWalletById;

/// <summary>
///     Query to get a wallet by its ID
/// </summary>
public record GetWalletByIdQuery(Guid WalletId) : IQuery<UserWallet?>;
