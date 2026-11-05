using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for PurgeUserCommand (issue #1287 U2: the 14 unvalidated commands).
/// </summary>
public sealed class PurgeUserCommandValidator : AbstractValidator<PurgeUserCommand>
{
    public PurgeUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.Strategy)
            .IsInEnum()
            .WithMessage("Purge strategy must be a known value.");
    }
}
