using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.QueryWallet;

/// <summary>Query to get wallet balance</summary>
public record GetWalletBalanceQuery(
    Guid UserId
) : IRequest<decimal>;
