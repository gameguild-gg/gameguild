using FluentValidation;

namespace GameGuild.Identity.Users;

public sealed class MarkNotificationAsUnreadCommandValidator : AbstractValidator<MarkNotificationAsUnreadCommand>
{
    public MarkNotificationAsUnreadCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
