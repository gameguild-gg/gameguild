using FluentValidation;

namespace GameGuild.Identity.Users;

public sealed class ReplaceUserAccessibilityPreferencesCommandValidator : AbstractValidator<ReplaceUserAccessibilityPreferencesCommand>
{
    public ReplaceUserAccessibilityPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.AccessibilityPreferences).NotNull();
    }
}
