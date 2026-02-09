using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for DeleteUserCommand
/// </summary>
public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator() { RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required."); }
}
