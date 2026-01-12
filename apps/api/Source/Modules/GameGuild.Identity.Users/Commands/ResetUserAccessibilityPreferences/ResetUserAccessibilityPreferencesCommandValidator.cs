using FluentValidation;

namespace GameGuild.Identity.Users;

public class ResetUserAccessibilityPreferencesCommandValidator : AbstractValidator<ResetUserAccessibilityPreferencesCommand>
{
    public ResetUserAccessibilityPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
