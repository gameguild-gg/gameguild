using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed record MarkNotificationAsUnreadCommand(Guid UserId, Guid NotificationId) : ICommand;
