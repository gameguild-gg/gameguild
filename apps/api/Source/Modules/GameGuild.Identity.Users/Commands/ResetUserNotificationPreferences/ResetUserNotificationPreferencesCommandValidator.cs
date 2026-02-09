using FluentValidation;

namespace GameGuild.Identity.Users;

public sealed class ResetUserNotificationPreferencesCommandValidator : AbstractValidator<ResetUserNotificationPreferencesCommand>
{
    public ResetUserNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
