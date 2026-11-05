using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for UnarchiveNotificationCommand (issue #1287 U2: the 14 unvalidated
///     commands — mirrors ArchiveNotificationCommandValidator).
/// </summary>
public sealed class UnarchiveNotificationCommandValidator : AbstractValidator<UnarchiveNotificationCommand>
{
    public UnarchiveNotificationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
