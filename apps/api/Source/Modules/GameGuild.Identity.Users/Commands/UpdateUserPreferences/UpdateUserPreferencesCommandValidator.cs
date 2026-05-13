using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for UpdateUserPreferencesCommand
/// </summary>
public sealed class UpdateUserPreferencesCommandValidator : AbstractValidator<UpdateUserPreferencesCommand>
{
    public UpdateUserPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID cannot be empty");

        RuleFor(x => x.Request)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Request cannot be null")
            .Must(r => r.GeneralPreferences != null || r.NotificationPreferences != null || r.AccessibilityPreferences != null || r.PrivacyPreferences != null)
            .WithMessage("At least one preference category must be provided");
    }
}
