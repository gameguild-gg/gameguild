using FluentValidation;

namespace GameGuild.Users.Commands;

/// <summary>
///     Validator for BulkDeactivateUsersCommand
/// </summary>
public class BulkDeactivateUsersCommandValidator : AbstractValidator<BulkDeactivateUsersCommand>
{
    public BulkDeactivateUsersCommandValidator()
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
