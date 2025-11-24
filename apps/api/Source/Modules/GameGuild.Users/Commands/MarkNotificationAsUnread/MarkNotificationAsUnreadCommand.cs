using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

public record MarkNotificationAsUnreadCommand(Guid UserId, Guid NotificationId) : ICommand;
