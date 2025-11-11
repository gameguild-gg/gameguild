using FluentValidation;

namespace GameGuild.Users.Commands;

/// <summary>
///     Validator for BulkCreateUsersCommand
/// </summary>
public class BulkCreateUsersCommandValidator : AbstractValidator<BulkCreateUsersCommand>
{
    public BulkCreateUsersCommandValidator()
    {
        RuleFor(x => x.Users).NotNull().WithMessage("Users collection is required.").NotEmpty().WithMessage("At least one user is required.");

        RuleForEach(x => x.Users).SetValidator(_ => new CreateUserRequestItemValidator());
    }
}
