using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for ReplaceUserLocalizationPreferencesCommand (issue #1287 U2: the 14
/// unvalidated commands — the localization family was the only preference family
/// without a FluentValidation gate).
/// </summary>
public sealed class ReplaceUserLocalizationPreferencesCommandValidator : AbstractValidator<ReplaceUserLocalizationPreferencesCommand>
{
    public ReplaceUserLocalizationPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID cannot be empty");

        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request cannot be null");

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.LocalizationPreferences)
                .NotNull()
                .WithMessage("Localization preferences cannot be null");
        });
    }
}
