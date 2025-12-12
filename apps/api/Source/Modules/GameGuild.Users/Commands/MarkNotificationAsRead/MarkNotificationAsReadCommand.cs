using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

public record MarkNotificationAsReadCommand(Guid UserId, Guid NotificationId) : ICommand;
