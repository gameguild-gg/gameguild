using FluentValidation;

namespace GameGuild.Users.Commands;

public class ResetUserAccessibilityPreferencesCommandValidator : AbstractValidator<ResetUserAccessibilityPreferencesCommand>
{
    public ResetUserAccessibilityPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
