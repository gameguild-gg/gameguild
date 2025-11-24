using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

public record ArchiveNotificationCommand(Guid UserId, Guid NotificationId) : ICommand;
