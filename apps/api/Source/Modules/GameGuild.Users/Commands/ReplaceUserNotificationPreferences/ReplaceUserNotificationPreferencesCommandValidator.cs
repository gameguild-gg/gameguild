using FluentValidation;

namespace GameGuild.Users.Commands;

public class ReplaceUserNotificationPreferencesCommandValidator : AbstractValidator<ReplaceUserNotificationPreferencesCommand>
{
    public ReplaceUserNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.NotificationPreferences).NotNull();
    }
}
