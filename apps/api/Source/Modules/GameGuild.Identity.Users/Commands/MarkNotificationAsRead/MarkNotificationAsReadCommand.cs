using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public record MarkNotificationAsReadCommand(Guid UserId, Guid NotificationId) : ICommand;
