using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.QueryWallet;

/// <summary>Query to get wallet by user ID</summary>
public record GetWalletByUserIdQuery(
    Guid UserId
) : IRequest<UserWallet?>;
