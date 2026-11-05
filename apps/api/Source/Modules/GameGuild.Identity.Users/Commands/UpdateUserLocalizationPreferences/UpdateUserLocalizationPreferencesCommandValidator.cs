using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for UpdateUserLocalizationPreferencesCommand (issue #1287 U2: the 14
/// unvalidated commands).
/// </summary>
public sealed class UpdateUserLocalizationPreferencesCommandValidator : AbstractValidator<UpdateUserLocalizationPreferencesCommand>
{
    public UpdateUserLocalizationPreferencesCommandValidator()
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
