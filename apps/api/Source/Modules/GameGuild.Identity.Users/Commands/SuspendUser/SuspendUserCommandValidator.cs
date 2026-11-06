using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for SuspendUserCommand (issue #1287 U2: the 14 unvalidated commands).
/// </summary>
public sealed class SuspendUserCommandValidator : AbstractValidator<SuspendUserCommand>
{
    public SuspendUserCommandValidator() { RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required."); }
}
