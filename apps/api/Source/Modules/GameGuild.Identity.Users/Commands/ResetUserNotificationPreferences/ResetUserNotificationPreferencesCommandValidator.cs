using FluentValidation;

namespace GameGuild.Identity.Users;

public class ResetUserNotificationPreferencesCommandValidator : AbstractValidator<ResetUserNotificationPreferencesCommand>
{
    public ResetUserNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
