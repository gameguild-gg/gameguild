using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public record ArchiveNotificationCommand(Guid UserId, Guid NotificationId) : ICommand;
