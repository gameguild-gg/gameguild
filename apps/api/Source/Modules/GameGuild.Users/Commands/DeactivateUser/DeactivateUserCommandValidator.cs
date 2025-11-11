using FluentValidation;

namespace GameGuild.Users.Commands;

/// <summary>
///     Validator for DeactivateUserCommand
/// </summary>
public class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator() { RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required."); }
}
