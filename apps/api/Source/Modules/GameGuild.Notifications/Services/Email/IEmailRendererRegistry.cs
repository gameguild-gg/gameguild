namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Resolves the <see cref="IEmailRenderer"/> responsible for a notification type.
/// </summary>
public interface IEmailRendererRegistry
{
    /// <summary>
    /// Gets the renderer registered for <paramref name="type"/>, or null when no renderer exists.
    /// Null is a permanent configuration error: the caller deadletters the row with a reason naming the type.
    /// </summary>
    IEmailRenderer? Resolve(NotificationType type);
}
