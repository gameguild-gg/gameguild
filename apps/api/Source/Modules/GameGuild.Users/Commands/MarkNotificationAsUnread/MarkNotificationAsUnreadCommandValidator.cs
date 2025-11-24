using FluentValidation;

namespace GameGuild.Users.Commands;

public class MarkNotificationAsUnreadCommandValidator : AbstractValidator<MarkNotificationAsUnreadCommand>
{
    public MarkNotificationAsUnreadCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
