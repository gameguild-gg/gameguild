using FluentValidation;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Adds a fraud blacklist (or manual whitelist override) entry for the acting
///     administrator's tenant (issue #409). The tenant comes from the actor context, never
///     from the request body. Requires the <c>payments:admin</c> permission.
/// </summary>
/// <param name="EntryType">Kind of value being listed.</param>
/// <param name="Value">Pattern to store; <c>*</c> wildcards supported, normalized on store.</param>
/// <param name="Reason">Bounded reason recorded with the entry.</param>
/// <param name="IsWhitelist">True for a manual whitelist override instead of a blacklist entry.</param>
/// <param name="ExpiresAt">Optional expiry instant; null = no expiry.</param>
[AuthorizeRequest(PaymentsPermission.Keys.Admin)]
public sealed record AddFraudBlacklistEntryCommand(
    FraudBlacklistEntryType EntryType,
    string Value,
    string? Reason = null,
    bool IsWhitelist = false,
    DateTime? ExpiresAt = null) : ICommand<FraudBlacklistEntryResult>;

/// <summary>Read model for a stored blacklist entry.</summary>
public sealed record FraudBlacklistEntryResult
{
    /// <summary>Entry id.</summary>
    public required Guid Id { get; init; }

    /// <summary>Owning tenant.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Entry kind.</summary>
    public required FraudBlacklistEntryType EntryType { get; init; }

    /// <summary>Stored (normalized) pattern.</summary>
    public required string Value { get; init; }

    /// <summary>Bounded reason, when recorded.</summary>
    public string? Reason { get; init; }

    /// <summary>Whether this is a whitelist override.</summary>
    public required bool IsWhitelist { get; init; }

    /// <summary>How the entry was added.</summary>
    public required FraudBlacklistSource Source { get; init; }

    /// <summary>Expiry instant; null = no expiry.</summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>When the entry was created.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>Map an entity to the read model.</summary>
    /// <param name="entry">Entity to map.</param>
    /// <returns>Read model.</returns>
    public static FraudBlacklistEntryResult From(FraudBlacklistEntry entry) => new()
    {
        Id = entry.Id,
        TenantId = entry.TenantId,
        EntryType = entry.EntryType,
        Value = entry.Value,
        Reason = entry.Reason,
        IsWhitelist = entry.IsWhitelist,
        Source = entry.Source,
        ExpiresAt = entry.ExpiresAt,
        CreatedAt = entry.CreatedAt
    };
}

/// <summary>Validator for <see cref="AddFraudBlacklistEntryCommand"/>.</summary>
public sealed class AddFraudBlacklistEntryCommandValidator : AbstractValidator<AddFraudBlacklistEntryCommand>
{
    /// <summary>Initializes the validator.</summary>
    public AddFraudBlacklistEntryCommandValidator()
    {
        RuleFor(command => command.EntryType)
            .IsInEnum()
            .WithMessage("entryType must be a known fraud blacklist entry type.");

        RuleFor(command => command.Value)
            .NotEmpty()
            .MaximumLength(255)
            .Must(value => !value.Any(char.IsWhiteSpace))
            .WithMessage("value must not contain whitespace; use wildcard patterns like *.malicious.com");

        RuleFor(command => command.Reason)
            .MaximumLength(1000);

        RuleFor(command => command.ExpiresAt)
            .Must(expiresAt => expiresAt is null || expiresAt.Value > SystemClock.UtcNow)
            .WithMessage("expiresAt must be in the future.");
    }
}

/// <summary>Handler for <see cref="AddFraudBlacklistEntryCommand"/>.</summary>
public sealed class AddFraudBlacklistEntryCommandHandler(
    IFraudBlacklistService blacklistService,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<AddFraudBlacklistEntryCommand, FraudBlacklistEntryResult>
{
    /// <inheritdoc />
    public async Task<FraudBlacklistEntryResult> Handle(AddFraudBlacklistEntryCommand request, CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        var tenantId = actor.TenantId
            ?? throw new UnauthorizedAccessException("A tenant context is required to manage the fraud blacklist.");

        var createdByUserId = actor.SubjectIdAsGuid;
        var entry = FraudBlacklistEntry.Create(
            tenantId,
            request.EntryType,
            request.Value,
            request.Reason,
            request.IsWhitelist,
            FraudBlacklistSource.Manual,
            request.ExpiresAt,
            createdByUserId)
            ?? throw new InvalidOperationException("The blacklist value is blank or exceeds 255 characters.");

        var stored = await blacklistService.AddAsync(entry, cancellationToken).ConfigureAwait(false);

        return FraudBlacklistEntryResult.From(stored);
    }
}
