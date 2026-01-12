using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for BulkUpdateUsersCommand
/// </summary>
public class BulkUpdateUsersCommandValidator : AbstractValidator<BulkUpdateUsersCommand>
{
    public BulkUpdateUsersCommandValidator()
    {
        RuleFor(x => x.Updates).NotNull().WithMessage("Updates collection is required.").NotEmpty().WithMessage("At least one update is required.");

        RuleForEach(x => x.Updates).SetValidator(_ => new UpdateUserRequestItemValidator());
    }
}
