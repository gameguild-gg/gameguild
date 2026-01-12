using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public record MarkNotificationAsUnreadCommand(Guid UserId, Guid NotificationId) : ICommand;
