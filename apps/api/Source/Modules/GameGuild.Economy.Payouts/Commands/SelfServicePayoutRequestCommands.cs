using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using GameGuild.CQRS;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Payouts.Queries;
using GameGuild.Economy.Queries;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Economy.Payouts.Commands;

public sealed record CreateMyPayoutRequestRequest(long HardCoinUnits, string IdempotencyKey);

public sealed record CreateMyPayoutRequestCommand(CreateMyPayoutRequestRequest Request)
    : ICommand<EconomyPayoutRequestDto>;

public sealed record CancelMyPayoutRequestCommand(Guid RequestId)
    : ICommand<EconomyPayoutRequestDto>;

public sealed class CreateMyPayoutRequestCommandValidator : AbstractValidator<CreateMyPayoutRequestCommand>
{
    public CreateMyPayoutRequestCommandValidator()
    {
        RuleFor(command => command.Request).NotNull();
        When(command => command.Request is not null, () =>
        {
            RuleFor(command => command.Request.HardCoinUnits).GreaterThan(0);
            RuleFor(command => command.Request.IdempotencyKey).NotEmpty().MaximumLength(128);
        });
    }
}

public sealed class CreateMyPayoutRequestCommandHandler(
    ISender sender,
    IActorContextAccessor actorContextAccessor,
    IPayoutRequestStore requests)
    : ICommandHandler<CreateMyPayoutRequestCommand, EconomyPayoutRequestDto>
{
    public async Task<EconomyPayoutRequestDto> Handle(
        CreateMyPayoutRequestCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);
        var actor = RequireActor(actorContextAccessor);
        var wallet = await sender.Send(new GetMyEconomyWalletQuery(), cancellationToken).ConfigureAwait(false)
            ?? throw new PayoutRequestWalletUnavailableException(
                "Create an Economy wallet before requesting a payout.");
        if (wallet.State != WalletLifecycleState.Active)
            throw new PayoutRequestWalletUnavailableException(
                "An active Economy wallet is required before requesting a payout.");
        var requestHash = ComputeRequestHash(actor, wallet.WalletId, command.Request.HardCoinUnits);
        var replay = requests.FindReplay(actor, command.Request.IdempotencyKey.Trim(), requestHash);
        if (replay is not null)
            return EconomyPayoutRequestDto.From(replay);
        if (command.Request.HardCoinUnits > wallet.WithdrawableHard)
            throw new PayoutRequestInsufficientWithdrawableFundsException(
                "The payout request exceeds HardCoin value that is confirmed and eligible for withdrawal.");

        var now = DateTimeOffset.UtcNow;
        var request = new PayoutRequest(
            Guid.NewGuid(),
            new IdempotencyKey(command.Request.IdempotencyKey.Trim()),
            requestHash,
            actor,
            new WalletId(wallet.WalletId),
            new CoinAmount(CurrencyCode.HardCoin, command.Request.HardCoinUnits),
            PayoutRequestState.Submitted,
            1,
            now,
            now);
        requests.Add(request);
        return EconomyPayoutRequestDto.From(request);
    }

    private static Guid RequireActor(IActorContextAccessor accessor)
    {
        var actor = accessor.ActorContext;
        return actor.IsAuthenticated && actor.SubjectIdAsGuid is { } actorId && actor.TenantId.HasValue
            ? actorId
            : throw new UnauthorizedAccessException("An authenticated tenant actor is required for payout requests.");
    }

    private static string ComputeRequestHash(Guid actorId, Guid walletId, long amountUnits)
    {
        var payload = $"{actorId:N}|{walletId:N}|{amountUnits}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}

public sealed class CancelMyPayoutRequestCommandHandler(
    IActorContextAccessor actorContextAccessor,
    IPayoutRequestStore requests)
    : ICommandHandler<CancelMyPayoutRequestCommand, EconomyPayoutRequestDto>
{
    public Task<EconomyPayoutRequestDto> Handle(
        CancelMyPayoutRequestCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || actor.SubjectIdAsGuid is not { } actorId || !actor.TenantId.HasValue)
            throw new UnauthorizedAccessException("An authenticated tenant actor is required for payout requests.");

        var current = requests.GetForPayee(command.RequestId, actorId);
        var cancelled = current.Cancel(DateTimeOffset.UtcNow);
        return Task.FromResult(EconomyPayoutRequestDto.From(requests.Update(cancelled, current.Version)));
    }
}
