using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for BulkUnsuspendUsersCommand
/// </summary>
public class BulkUnsuspendUsersCommandValidator : AbstractValidator<BulkUnsuspendUsersCommand>
{
    public BulkUnsuspendUsersCommandValidator()
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
