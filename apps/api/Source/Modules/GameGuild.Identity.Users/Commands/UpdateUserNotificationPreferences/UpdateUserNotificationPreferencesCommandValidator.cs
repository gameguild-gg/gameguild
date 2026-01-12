using FluentValidation;

namespace GameGuild.Identity.Users;

public class UpdateUserNotificationPreferencesCommandValidator : AbstractValidator<UpdateUserNotificationPreferencesCommand>
{
    public UpdateUserNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.NotificationPreferences).NotNull().NotEmpty();
    }
}
