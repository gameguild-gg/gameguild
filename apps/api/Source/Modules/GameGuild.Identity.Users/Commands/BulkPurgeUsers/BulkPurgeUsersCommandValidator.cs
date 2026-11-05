using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for BulkPurgeUsersCommand (issue #1287 U2: the 14 unvalidated commands —
///     both purge paths get a FluentValidation gate).
/// </summary>
public sealed class BulkPurgeUsersCommandValidator : AbstractValidator<BulkPurgeUsersCommand>
{
    public BulkPurgeUsersCommandValidator()
    {
        RuleFor(x => x.UserIds)
            .NotNull()
            .WithMessage("User IDs collection cannot be null")
            .NotEmpty()
            .WithMessage("At least one user ID must be provided");

        RuleForEach(x => x.UserIds)
            .NotEmpty()
            .WithMessage("All user IDs must be valid (non-empty GUIDs)");

        RuleFor(x => x.Strategy)
            .IsInEnum()
            .WithMessage("Purge strategy must be a known value.");
    }
}
