using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for ActivateUserCommand
/// </summary>
public class ActivateUserCommandValidator : AbstractValidator<ActivateUserCommand>
{
    public ActivateUserCommandValidator() { RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required."); }
}
