using FluentValidation;

namespace GameGuild.Users.Commands;

/// <summary>
///     Validator for DeleteUserCommand
/// </summary>
public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator() { RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required."); }
}
