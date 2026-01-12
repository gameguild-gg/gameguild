using FluentValidation;

namespace GameGuild.Identity.Users;

public class ReplaceUserNotificationPreferencesCommandValidator : AbstractValidator<ReplaceUserNotificationPreferencesCommand>
{
    public ReplaceUserNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.NotificationPreferences).NotNull();
    }
}
