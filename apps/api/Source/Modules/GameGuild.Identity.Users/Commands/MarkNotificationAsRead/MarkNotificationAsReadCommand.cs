using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed record MarkNotificationAsReadCommand(Guid UserId, Guid NotificationId) : ICommand;
