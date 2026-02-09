using FluentValidation;

namespace GameGuild.Identity.Users;

public sealed class ArchiveNotificationCommandValidator : AbstractValidator<ArchiveNotificationCommand>
{
    public ArchiveNotificationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
