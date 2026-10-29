using FluentValidation;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Commerce.Payments;

/// <summary>Verification state of a saved bank-account payment method (issue #414).</summary>
/// <param name="PaymentMethodId">Bank-account payment method the challenge belongs to.</param>
/// <param name="Status">One of <c>none</c>, <c>pending</c>, <c>verified</c>, <c>expired</c>, <c>locked</c>.</param>
/// <param name="InitiatedAtUtc">UTC instant the two deposits were generated; null when no challenge exists.</param>
/// <param name="ExpiresAtUtc">UTC deadline (3 business days); null when no challenge exists.</param>
/// <param name="VerifiedAtUtc">UTC instant of the successful echo; null until verified.</param>
/// <param name="FailedAttempts">Failed echo attempts against the current challenge.</param>
/// <param name="RemainingAttempts">Echo attempts left before the challenge locks.</param>
public sealed record BankAccountVerificationStatusDto(
    Guid PaymentMethodId,
    string Status,
    DateTime? InitiatedAtUtc,
    DateTime? ExpiresAtUtc,
    DateTime? VerifiedAtUtc,
    int FailedAttempts,
    int RemainingAttempts);

/// <summary>
///     Initiation result (issue #414): the challenge status plus the two generated deposit
///     amounts. The amounts are surfaced once here — the same visibility a gateway sandbox
///     gives the operator — and never again: the stored challenge keeps only salted hashes.
/// </summary>
/// <param name="Status">Challenge status after initiation.</param>
/// <param name="Deposit1Amount">First generated deposit amount.</param>
/// <param name="Deposit2Amount">Second generated deposit amount.</param>
public sealed record BankAccountVerificationInitiationDto(
    BankAccountVerificationStatusDto Status,
    decimal Deposit1Amount,
    decimal Deposit2Amount);

/// <summary>
///     Command (issue #414): initiate micro-deposit verification for a saved bank-account
///     payment method. Generates two random deposits ($0.01–$0.99) and stores only their
///     salted hashes; the owner has 3 business days and at most 3 failed attempts. Idempotent
///     while an open challenge exists (returns its status without regenerating); a new
///     challenge may be initiated after expiry or lock.
/// </summary>
/// <param name="PaymentMethodId">Bank-account payment method to verify.</param>
[AuthorizeRequest("", RequireTenantAdmin = false)]
public sealed record InitiateBankAccountVerificationCommand(Guid PaymentMethodId) : ICommand<Result<BankAccountVerificationInitiationDto>>;

/// <summary>Validator for <see cref="InitiateBankAccountVerificationCommand"/>.</summary>
public sealed class InitiateBankAccountVerificationCommandValidator : AbstractValidator<InitiateBankAccountVerificationCommand>
{
    /// <summary>Initializes the validator.</summary>
    public InitiateBankAccountVerificationCommandValidator()
    {
        RuleFor(command => command.PaymentMethodId).NotEmpty();
    }
}

/// <summary>
///     Command (issue #414): echo the two micro-deposit amounts observed on the bank
///     statement. Both amounts must match within the challenge deadline and the attempt cap;
///     success marks the bank account verified (and therefore chargeable).
/// </summary>
/// <param name="PaymentMethodId">Bank-account payment method being verified.</param>
/// <param name="Deposit1Amount">First observed deposit amount.</param>
/// <param name="Deposit2Amount">Second observed deposit amount.</param>
[AuthorizeRequest("", RequireTenantAdmin = false)]
public sealed record VerifyBankAccountDepositsCommand(
    Guid PaymentMethodId,
    decimal Deposit1Amount,
    decimal Deposit2Amount) : ICommand<Result<BankAccountVerificationStatusDto>>;

/// <summary>Validator for <see cref="VerifyBankAccountDepositsCommand"/>.</summary>
public sealed class VerifyBankAccountDepositsCommandValidator : AbstractValidator<VerifyBankAccountDepositsCommand>
{
    /// <summary>Initializes the validator.</summary>
    public VerifyBankAccountDepositsCommandValidator()
    {
        RuleFor(command => command.PaymentMethodId).NotEmpty();
        RuleFor(command => command.Deposit1Amount)
            .InclusiveBetween(BankAccountVerificationRules.MinimumAmount, BankAccountVerificationRules.MaximumAmount)
            .WithMessage($"Deposit amounts must be between {BankAccountVerificationRules.MinimumAmount:0.00} and {BankAccountVerificationRules.MaximumAmount:0.00}.");
        RuleFor(command => command.Deposit2Amount)
            .InclusiveBetween(BankAccountVerificationRules.MinimumAmount, BankAccountVerificationRules.MaximumAmount)
            .WithMessage($"Deposit amounts must be between {BankAccountVerificationRules.MinimumAmount:0.00} and {BankAccountVerificationRules.MaximumAmount:0.00}.");
    }
}

/// <summary>Shared handler logic for the verification commands.</summary>
public sealed class BankAccountVerificationCommandHandler(
    IPaymentMethodRepository paymentMethods,
    IBankAccountVerificationRepository verifications,
    IActorContextAccessor actorContextAccessor,
    IRevenueAuditService? revenueAuditService = null)
{
    /// <summary>Time source for challenge deadlines (overridable in unit tests).</summary>
    internal Func<DateTime> UtcNow { get; init; } = static () => SystemClock.UtcNow;

    /// <summary>Handles <see cref="InitiateBankAccountVerificationCommand"/>.</summary>
    public async Task<Result<BankAccountVerificationInitiationDto>> Handle(InitiateBankAccountVerificationCommand request, CancellationToken cancellationToken)
    {
        var (actor, methodError) = await ResolveBankAccountAsync(request.PaymentMethodId, cancellationToken).ConfigureAwait(false);
        if (methodError is not null)
        {
            return Result.Failure<BankAccountVerificationInitiationDto>(methodError);
        }

        var now = UtcNow();
        var existing = await verifications.GetLatestAsync(request.PaymentMethodId, cancellationToken).ConfigureAwait(false);
        if (existing is { IsVerified: false } open && open.IsOpen(now) && !open.IsLocked)
        {
            // Idempotent while an open challenge exists: never regenerate deposits silently.
            return Result.Failure<BankAccountVerificationInitiationDto>(Error.Conflict(
                "PaymentMethods.BankVerificationPending",
                $"A micro-deposit challenge is already open for this bank account (initiated {open.InitiatedAtUtc:yyyy-MM-dd}, deadline {open.ExpiresAtUtc:yyyy-MM-dd}). Echo the two deposit amounts to verify it."));
        }

        var (deposit1, deposit2, salt) = BankAccountVerificationRules.GenerateChallenge();
        var verification = new BankAccountVerification(
            methodTenantId(actor),
            request.PaymentMethodId,
            BankAccountVerificationRules.HashDeposit(salt, deposit1),
            BankAccountVerificationRules.HashDeposit(salt, deposit2),
            salt,
            now);
        await verifications.AddAsync(verification, cancellationToken).ConfigureAwait(false);

        await RecordAuditAsync(actor, request.PaymentMethodId, "BankVerificationInitiated", now, cancellationToken).ConfigureAwait(false);

        return Result.Success(new BankAccountVerificationInitiationDto(
            ToStatus(verification, now),
            deposit1,
            deposit2));
    }

    /// <summary>Handles <see cref="VerifyBankAccountDepositsCommand"/>.</summary>
    public async Task<Result<BankAccountVerificationStatusDto>> Handle(VerifyBankAccountDepositsCommand request, CancellationToken cancellationToken)
    {
        var (actor, methodError) = await ResolveBankAccountAsync(request.PaymentMethodId, cancellationToken).ConfigureAwait(false);
        if (methodError is not null)
        {
            return Result.Failure<BankAccountVerificationStatusDto>(methodError);
        }

        var now = UtcNow();
        var verification = await verifications.GetLatestAsync(request.PaymentMethodId, cancellationToken).ConfigureAwait(false);
        if (verification is null)
        {
            return Result.Failure<BankAccountVerificationStatusDto>(Error.Conflict(
                "PaymentMethods.BankVerificationNotInitiated",
                "No micro-deposit challenge exists for this bank account. Initiate one first."));
        }

        if (verification.IsVerified)
        {
            return Result.Success(ToStatus(verification, now));
        }

        if (!verification.IsOpen(now))
        {
            return Result.Failure<BankAccountVerificationStatusDto>(Error.Conflict(
                "PaymentMethods.BankVerificationExpired",
                $"The micro-deposit challenge expired on {verification.ExpiresAtUtc:yyyy-MM-dd}. Initiate a new one."));
        }

        if (verification.IsLocked)
        {
            return Result.Failure<BankAccountVerificationStatusDto>(Error.Conflict(
                "PaymentMethods.BankVerificationLocked",
                $"The micro-deposit challenge locked after {BankAccountVerificationRules.MaxFailedAttempts} failed attempts. Initiate a new one."));
        }

        var matches =
            BankAccountVerificationRules.HashDeposit(verification.Salt, request.Deposit1Amount) == verification.Deposit1Hash
            && BankAccountVerificationRules.HashDeposit(verification.Salt, request.Deposit2Amount) == verification.Deposit2Hash;

        if (!matches)
        {
            verification.RecordFailedAttempt();
            await verifications.UpdateAsync(verification, cancellationToken).ConfigureAwait(false);
            await RecordAuditAsync(actor, request.PaymentMethodId, "BankVerificationAttemptFailed", now, cancellationToken).ConfigureAwait(false);

            return Result.Failure<BankAccountVerificationStatusDto>(Error.Validation(
                "PaymentMethods.BankVerificationAmountsMismatched",
                verification.IsLocked
                    ? $"The echoed amounts do not match. The challenge is now locked after {BankAccountVerificationRules.MaxFailedAttempts} failed attempts; initiate a new one."
                    : $"The echoed amounts do not match. {BankAccountVerificationRules.MaxFailedAttempts - verification.FailedAttempts} attempt(s) remain."));
        }

        verification.MarkVerified(now);
        await verifications.UpdateAsync(verification, cancellationToken).ConfigureAwait(false);
        await RecordAuditAsync(actor, request.PaymentMethodId, "BankVerificationCompleted", now, cancellationToken).ConfigureAwait(false);

        return Result.Success(ToStatus(verification, now));
    }

    private async Task<(PaymentMethodActor? Actor, Error? Error)> ResolveBankAccountAsync(Guid paymentMethodId, CancellationToken cancellationToken)
    {
        var actor = PaymentMethodActors.Resolve(actorContextAccessor);
        if (actor is null)
        {
            return (null, PaymentMethodActors.InvalidActor());
        }

        var method = await paymentMethods.GetByIdAsync(paymentMethodId, cancellationToken).ConfigureAwait(false);
        if (method is null)
        {
            return (actor, Error.NotFound("PaymentMethods.NotFound", $"Payment method {paymentMethodId} was not found."));
        }

        // Owner-only: verifying a bank account is the account holder's action.
        if (!actor.IsSystemAdmin && (actor.TenantId != method.TenantId || actor.UserId != method.UserId))
        {
            return (actor, Error.Forbidden(
                "PaymentMethods.NotOwner",
                "Only the payment method owner can verify its bank account."));
        }

        if (method.Type != PaymentMethodType.BankAccount)
        {
            return (actor, Error.Conflict(
                "PaymentMethods.NotABankAccount",
                "Micro-deposit verification applies to bank-account payment methods only."));
        }

        return (actor, null);
    }

    private static BankAccountVerificationStatusDto ToStatus(BankAccountVerification verification, DateTime utcNow)
    {
        var status = verification.IsVerified
            ? "verified"
            : verification.IsLocked
                ? "locked"
                : verification.IsOpen(utcNow)
                    ? "pending"
                    : "expired";
        return new BankAccountVerificationStatusDto(
            verification.PaymentMethodId,
            status,
            verification.InitiatedAtUtc,
            verification.ExpiresAtUtc,
            verification.VerifiedAtUtc,
            verification.FailedAttempts,
            Math.Max(0, BankAccountVerificationRules.MaxFailedAttempts - verification.FailedAttempts));
    }

    private async Task RecordAuditAsync(PaymentMethodActor? actor, Guid paymentMethodId, string action, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (revenueAuditService is null || actor is null)
        {
            return;
        }

        await revenueAuditService.RecordAuditTrailAsync(
            entityType: nameof(PaymentMethod),
            entityId: paymentMethodId,
            action: action,
            changedBy: actor.UserId,
            oldValue: null,
            newValue: null,
            reason: action switch
            {
                "BankVerificationInitiated" => "Micro-deposit challenge generated for a bank-account payment method",
                "BankVerificationAttemptFailed" => "Micro-deposit echo did not match the challenge",
                "BankVerificationCompleted" => "Bank-account payment method verified through micro-deposits",
                _ => action
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static Guid? methodTenantId(PaymentMethodActor? actor) => actor?.TenantId;
}

/// <summary>
///     MediatR-style handler adapters so the two commands dispatch through the pipeline
/// (the shared logic lives in <see cref="BankAccountVerificationCommandHandler"/>).
/// </summary>
public sealed class InitiateBankAccountVerificationCommandHandler(BankAccountVerificationCommandHandler inner)
    : ICommandHandler<InitiateBankAccountVerificationCommand, Result<BankAccountVerificationInitiationDto>>
{
    /// <inheritdoc />
    public Task<Result<BankAccountVerificationInitiationDto>> Handle(InitiateBankAccountVerificationCommand request, CancellationToken cancellationToken)
        => inner.Handle(request, cancellationToken);
}

/// <summary>Handler adapter for <see cref="VerifyBankAccountDepositsCommand"/>.</summary>
public sealed class VerifyBankAccountDepositsCommandHandler(BankAccountVerificationCommandHandler inner)
    : ICommandHandler<VerifyBankAccountDepositsCommand, Result<BankAccountVerificationStatusDto>>
{
    /// <inheritdoc />
    public Task<Result<BankAccountVerificationStatusDto>> Handle(VerifyBankAccountDepositsCommand request, CancellationToken cancellationToken)
        => inner.Handle(request, cancellationToken);
}

/// <summary>
///     Query (issue #414): current micro-deposit verification state of a saved bank-account
///     payment method. Owner-only.
/// </summary>
/// <param name="PaymentMethodId">Bank-account payment method.</param>
public sealed record GetBankAccountVerificationQuery(Guid PaymentMethodId) : IQuery<Result<BankAccountVerificationStatusDto>>;

/// <summary>Validator for <see cref="GetBankAccountVerificationQuery"/>.</summary>
public sealed class GetBankAccountVerificationQueryValidator : AbstractValidator<GetBankAccountVerificationQuery>
{
    /// <summary>Initializes the validator.</summary>
    public GetBankAccountVerificationQueryValidator()
    {
        RuleFor(query => query.PaymentMethodId).NotEmpty();
    }
}

/// <summary>Handler for <see cref="GetBankAccountVerificationQuery"/>.</summary>
public sealed class GetBankAccountVerificationQueryHandler(
    IPaymentMethodRepository paymentMethods,
    IBankAccountVerificationRepository verifications,
    IActorContextAccessor actorContextAccessor) : IQueryHandler<GetBankAccountVerificationQuery, Result<BankAccountVerificationStatusDto>>
{
    /// <summary>Time source for challenge deadlines (overridable in unit tests).</summary>
    internal Func<DateTime> UtcNow { get; init; } = static () => SystemClock.UtcNow;

    /// <inheritdoc />
    public async Task<Result<BankAccountVerificationStatusDto>> Handle(GetBankAccountVerificationQuery request, CancellationToken cancellationToken)
    {
        var actor = PaymentMethodActors.Resolve(actorContextAccessor);
        if (actor is null)
        {
            return Result.Failure<BankAccountVerificationStatusDto>(PaymentMethodActors.InvalidActor());
        }

        var method = await paymentMethods.GetByIdAsync(request.PaymentMethodId, cancellationToken).ConfigureAwait(false);
        if (method is null)
        {
            return Result.Failure<BankAccountVerificationStatusDto>(Error.NotFound(
                "PaymentMethods.NotFound",
                $"Payment method {request.PaymentMethodId} was not found."));
        }

        if (!actor.IsSystemAdmin && (actor.TenantId != method.TenantId || actor.UserId != method.UserId))
        {
            return Result.Failure<BankAccountVerificationStatusDto>(Error.Forbidden(
                "PaymentMethods.NotOwner",
                "Only the payment method owner can read its bank-account verification state."));
        }

        var now = UtcNow();
        var verification = await verifications.GetLatestAsync(request.PaymentMethodId, cancellationToken).ConfigureAwait(false);
        if (verification is null)
        {
            return Result.Success(new BankAccountVerificationStatusDto(
                request.PaymentMethodId,
                "none",
                null, null, null,
                FailedAttempts: 0,
                RemainingAttempts: BankAccountVerificationRules.MaxFailedAttempts));
        }

        var status = verification.IsVerified
            ? "verified"
            : verification.IsLocked
                ? "locked"
                : verification.IsOpen(now)
                    ? "pending"
                    : "expired";
        return Result.Success(new BankAccountVerificationStatusDto(
            verification.PaymentMethodId,
            status,
            verification.InitiatedAtUtc,
            verification.ExpiresAtUtc,
            verification.VerifiedAtUtc,
            verification.FailedAttempts,
            Math.Max(0, BankAccountVerificationRules.MaxFailedAttempts - verification.FailedAttempts)));
    }
}
