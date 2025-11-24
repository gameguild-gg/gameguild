using FluentValidation;

namespace GameGuild.Users.Commands;

public class UpdateUserAccessibilityPreferencesCommandValidator : AbstractValidator<UpdateUserAccessibilityPreferencesCommand>
{
    public UpdateUserAccessibilityPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.AccessibilityPreferences).NotNull().NotEmpty();
    }
}
