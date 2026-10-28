using FluentValidation;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Checks whether a concrete value matches the tenant's fraud blacklist (issue #409):
///     the same evaluation the payment fraud gate uses, exposed for admin tooling and
///     pre-checks. Requires the <c>payments:admin</c> permission; the tenant scope is
///     enforced at the API boundary from the actor context.
/// </summary>
/// <param name="TenantId">Tenant whose entries are evaluated (from the actor context at the API boundary).</param>
/// <param name="EntryType">Entry kind to evaluate.</param>
/// <param name="Value">Concrete value to check.</param>
[AuthorizeRequest(PaymentsPermission.Keys.Admin)]
public sealed record CheckFraudBlacklistQuery(
    Guid TenantId,
    FraudBlacklistEntryType EntryType,
    string Value) : IQuery<FraudBlacklistCheckResult>;

/// <summary>Result of a blacklist check.</summary>
public sealed record FraudBlacklistCheckResult
{
    /// <summary>Match decision (NoMatch / Blacklisted / Whitelisted).</summary>
    public required FraudBlacklistEvaluation Evaluation { get; init; }

    /// <summary>Entry kind that was evaluated.</summary>
    public required FraudBlacklistEntryType EntryType { get; init; }

    /// <summary>Stored pattern that matched, when blacklisted.</summary>
    public string? MatchedPattern { get; init; }

    /// <summary>Bounded reason recorded with the matching entry.</summary>
    public string? Reason { get; init; }

    /// <summary>How the matching entry was added.</summary>
    public FraudBlacklistSource? Source { get; init; }

    /// <summary>Expiry of the matching entry; null = no expiry.</summary>
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>Validator for <see cref="CheckFraudBlacklistQuery"/>.</summary>
public sealed class CheckFraudBlacklistQueryValidator : AbstractValidator<CheckFraudBlacklistQuery>
{
    /// <summary>Initializes the validator.</summary>
    public CheckFraudBlacklistQueryValidator()
    {
        RuleFor(query => query.EntryType)
            .IsInEnum()
            .WithMessage("entryType must be a known fraud blacklist entry type.");

        RuleFor(query => query.Value)
            .NotEmpty()
            .MaximumLength(255);
    }
}

/// <summary>Handler for <see cref="CheckFraudBlacklistQuery"/>.</summary>
public sealed class CheckFraudBlacklistQueryHandler(IFraudBlacklistService blacklistService)
    : IQueryHandler<CheckFraudBlacklistQuery, FraudBlacklistCheckResult>
{
    /// <inheritdoc />
    public async Task<FraudBlacklistCheckResult> Handle(CheckFraudBlacklistQuery request, CancellationToken cancellationToken)
    {
        var match = await blacklistService
            .EvaluateAsync(request.TenantId, request.EntryType, request.Value, cancellationToken)
            .ConfigureAwait(false);

        return new FraudBlacklistCheckResult
        {
            Evaluation = match.Evaluation,
            EntryType = request.EntryType,
            MatchedPattern = match.MatchedPattern,
            Reason = match.Reason,
            Source = match.Source,
            ExpiresAt = match.ExpiresAt
        };
    }
}
