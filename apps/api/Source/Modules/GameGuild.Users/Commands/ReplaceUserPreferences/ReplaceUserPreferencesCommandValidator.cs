using FluentValidation;

namespace GameGuild.Users.Commands;

/// <summary>
///     Validator for ReplaceUserPreferencesCommand
/// </summary>
public class ReplaceUserPreferencesCommandValidator : AbstractValidator<ReplaceUserPreferencesCommand>
{
    public ReplaceUserPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID cannot be empty");

        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request cannot be null");

        RuleFor(x => x.Request.GeneralPreferences)
            .NotNull()
            .WithMessage("General preferences cannot be null");

        RuleFor(x => x.Request.NotificationPreferences)
            .NotNull()
            .WithMessage("Notification preferences cannot be null");

        RuleFor(x => x.Request.AccessibilityPreferences)
            .NotNull()
            .WithMessage("Accessibility preferences cannot be null");

        RuleFor(x => x.Request.PrivacyPreferences)
            .NotNull()
            .WithMessage("Privacy preferences cannot be null");
    }
}
