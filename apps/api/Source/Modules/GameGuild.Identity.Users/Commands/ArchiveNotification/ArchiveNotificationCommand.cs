using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed record ArchiveNotificationCommand(Guid UserId, Guid NotificationId) : ICommand;
