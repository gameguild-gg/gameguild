using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageWallet;

/// <summary>Command to create a new user wallet</summary>
public record CreateWalletCommand(
    Guid UserId,
    string Currency = "USD"
) : IRequest<UserWallet>;
