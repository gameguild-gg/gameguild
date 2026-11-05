using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for ResetUserLocalizationPreferencesCommand (issue #1287 U2: the 14
/// unvalidated commands).
/// </summary>
public sealed class ResetUserLocalizationPreferencesCommandValidator : AbstractValidator<ResetUserLocalizationPreferencesCommand>
{
    public ResetUserLocalizationPreferencesCommandValidator() { RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID cannot be empty"); }
}
