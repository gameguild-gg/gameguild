using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for RestoreUserCommand (issue #1287 U2: the 14 unvalidated commands).
/// </summary>
public sealed class RestoreUserCommandValidator : AbstractValidator<RestoreUserCommand>
{
    public RestoreUserCommandValidator() { RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required."); }
}
