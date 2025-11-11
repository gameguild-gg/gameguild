using FluentValidation;

namespace GameGuild.Users.Commands;

public class ResetUserNotificationPreferencesCommandValidator : AbstractValidator<ResetUserNotificationPreferencesCommand>
{
    public ResetUserNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
