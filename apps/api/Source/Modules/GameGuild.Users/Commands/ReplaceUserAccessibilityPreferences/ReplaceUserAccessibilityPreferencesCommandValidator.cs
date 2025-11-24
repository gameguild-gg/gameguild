using FluentValidation;

namespace GameGuild.Users.Commands;

public class ReplaceUserAccessibilityPreferencesCommandValidator : AbstractValidator<ReplaceUserAccessibilityPreferencesCommand>
{
    public ReplaceUserAccessibilityPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.AccessibilityPreferences).NotNull();
    }
}
