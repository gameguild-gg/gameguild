using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for BulkArchiveNotificationsCommand (issue #1287 U2: the 14 unvalidated commands).
/// </summary>
public sealed class BulkArchiveNotificationsCommandValidator : AbstractValidator<BulkArchiveNotificationsCommand>
{
    public BulkArchiveNotificationsCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.NotificationIds)
            .NotNull()
            .WithMessage("Notification IDs collection cannot be null")
            .NotEmpty()
            .WithMessage("At least one notification ID must be provided");

        RuleForEach(x => x.NotificationIds)
            .NotEmpty()
            .WithMessage("All notification IDs must be valid (non-empty GUIDs)");
    }
}
