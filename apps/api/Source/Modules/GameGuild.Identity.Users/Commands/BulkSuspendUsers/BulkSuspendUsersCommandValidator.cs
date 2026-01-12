using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for BulkSuspendUsersCommand
/// </summary>
public class BulkSuspendUsersCommandValidator : AbstractValidator<BulkSuspendUsersCommand>
{
    public BulkSuspendUsersCommandValidator()
    {
        RuleFor(x => x.UserIds)
            .NotNull()
            .WithMessage("User IDs collection cannot be null")
            .NotEmpty()
            .WithMessage("At least one user ID must be provided")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("All user IDs must be valid (non-empty GUIDs)");
    }
}
