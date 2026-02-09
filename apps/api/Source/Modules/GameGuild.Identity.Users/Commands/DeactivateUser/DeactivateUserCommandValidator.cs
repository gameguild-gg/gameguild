using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for DeactivateUserCommand
/// </summary>
public sealed class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator() { RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required."); }
}
