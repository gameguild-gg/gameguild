namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Resolves the destination email address for an email-channel notification.
/// </summary>
public interface IRecipientEmailResolver
{
    /// <summary>
    /// RecipientEmail column wins; otherwise RecipientId is looked up against the user store.
    /// Returns null when neither yields a usable address (a permanent delivery error).
    /// </summary>
    Task<string?> ResolveAsync(Notification notification, CancellationToken cancellationToken = default);
}
