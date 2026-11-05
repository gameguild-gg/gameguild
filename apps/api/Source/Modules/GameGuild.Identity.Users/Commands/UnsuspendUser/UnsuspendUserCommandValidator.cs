using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for UnsuspendUserCommand (issue #1287 U2: the 14 unvalidated commands).
/// </summary>
public sealed class UnsuspendUserCommandValidator : AbstractValidator<UnsuspendUserCommand>
{
    public UnsuspendUserCommandValidator() { RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required."); }
}
