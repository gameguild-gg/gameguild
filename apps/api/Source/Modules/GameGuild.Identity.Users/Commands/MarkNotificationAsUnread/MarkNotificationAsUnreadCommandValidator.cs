using FluentValidation;

namespace GameGuild.Identity.Users;

public class MarkNotificationAsUnreadCommandValidator : AbstractValidator<MarkNotificationAsUnreadCommand>
{
    public MarkNotificationAsUnreadCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
