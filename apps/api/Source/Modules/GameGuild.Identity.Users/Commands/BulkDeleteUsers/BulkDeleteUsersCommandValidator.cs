using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for BulkDeleteUsersCommand
/// </summary>
public class BulkDeleteUsersCommandValidator : AbstractValidator<BulkDeleteUsersCommand>
{
    public BulkDeleteUsersCommandValidator()
    {
        RuleFor(x => x.UserIds)
            .NotNull()
            .WithMessage("User IDs collection is required.")
            .NotEmpty()
            .WithMessage("At least one user ID is required.")
            .Must(userIds => userIds.All(id => id != Guid.Empty))
            .WithMessage("All user IDs must be valid (non-empty GUIDs).");
    }
}
