using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for ResetUserPreferencesCommand
/// </summary>
public class ResetUserPreferencesCommandValidator : AbstractValidator<ResetUserPreferencesCommand>
{
    public ResetUserPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID cannot be empty");
    }
}
