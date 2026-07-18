using FluentValidation;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Resources;

namespace GameGuild.Commerce.Payments;

[AuthorizeRequest("")]
[RequiresQuota(ResourceUsageType.Wallets, 1, Source = "CreateMyWallet")]
public sealed record CreateMyWalletCommand(string Currency = "USD") : ICommand<UserWallet>;

[AuthorizeRequest("")]
public sealed record GetMyWalletQuery : IQuery<UserWallet?>;

[AuthorizeRequest("")]
public sealed record GetMyWalletBalanceQuery : IQuery<decimal>;

[AuthorizeRequest("")]
public sealed record LockMyWalletCommand(string Reason) : ICommand;

[AuthorizeRequest("")]
public sealed record UnlockMyWalletCommand : ICommand;

public sealed class CreateMyWalletCommandHandler(
    IWalletService walletService,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<CreateMyWalletCommand, UserWallet>
{
    public Task<UserWallet> Handle(CreateMyWalletCommand request, CancellationToken cancellationToken) =>
        walletService.CreateWalletAsync(SelfWalletActor.RequireUserId(actorContextAccessor), request.Currency, cancellationToken);
}

public sealed class GetMyWalletQueryHandler(
    IWalletService walletService,
    IActorContextAccessor actorContextAccessor) : IQueryHandler<GetMyWalletQuery, UserWallet?>
{
    public Task<UserWallet?> Handle(GetMyWalletQuery request, CancellationToken cancellationToken) =>
        walletService.GetWalletByUserIdAsync(SelfWalletActor.RequireUserId(actorContextAccessor), cancellationToken);
}

public sealed class GetMyWalletBalanceQueryHandler(
    IWalletService walletService,
    IActorContextAccessor actorContextAccessor) : IQueryHandler<GetMyWalletBalanceQuery, decimal>
{
    public Task<decimal> Handle(GetMyWalletBalanceQuery request, CancellationToken cancellationToken) =>
        walletService.GetBalanceAsync(SelfWalletActor.RequireUserId(actorContextAccessor), cancellationToken);
}

public sealed class LockMyWalletCommandHandler(
    IWalletService walletService,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<LockMyWalletCommand>
{
    public async Task<Unit> Handle(LockMyWalletCommand request, CancellationToken cancellationToken)
    {
        await walletService.LockWalletAsync(
            SelfWalletActor.RequireUserId(actorContextAccessor),
            request.Reason,
            cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public sealed class UnlockMyWalletCommandHandler(
    IWalletService walletService,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<UnlockMyWalletCommand>
{
    public async Task<Unit> Handle(UnlockMyWalletCommand request, CancellationToken cancellationToken)
    {
        await walletService.UnlockWalletAsync(
            SelfWalletActor.RequireUserId(actorContextAccessor),
            cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public sealed class CreateMyWalletCommandValidator : AbstractValidator<CreateMyWalletCommand>
{
    public CreateMyWalletCommandValidator()
    {
        RuleFor(command => command.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$");
    }
}

public sealed class LockMyWalletCommandValidator : AbstractValidator<LockMyWalletCommand>
{
    public LockMyWalletCommandValidator()
    {
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}

internal static class SelfWalletActor
{
    public static Guid RequireUserId(IActorContextAccessor actorContextAccessor)
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || !actor.SubjectIdAsGuid.HasValue)
        {
            throw new UnauthorizedAccessException("An authenticated user context is required.");
        }

        if (!actor.TenantId.HasValue)
        {
            throw new UnauthorizedAccessException("A tenant context is required.");
        }

        return actor.SubjectIdAsGuid.Value;
    }
}
