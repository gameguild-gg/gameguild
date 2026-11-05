using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for BulkRestoreUsersCommand (issue #1287 U2: the 14 unvalidated commands).
/// </summary>
public sealed class BulkRestoreUsersCommandValidator : AbstractValidator<BulkRestoreUsersCommand>
{
    public BulkRestoreUsersCommandValidator()
    {
        RuleFor(x => x.UserIds)
            .NotNull()
            .WithMessage("User IDs collection cannot be null")
            .NotEmpty()
            .WithMessage("At least one user ID must be provided");

        RuleForEach(x => x.UserIds)
            .NotEmpty()
            .WithMessage("All user IDs must be valid (non-empty GUIDs)");
    }
}
