namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Base class for email renderers (T6-T9). Provides the common footer-merge helper and the
/// suppressible/transactional guards so concrete renderers stay thin. Renderers receive
/// <see cref="IEmailFooterService"/> via constructor injection and call <see cref="MergeFooter"/>
/// to append the unsubscribe footer to their body for suppressible types.
/// </summary>
public abstract class EmailRendererBase
{
    /// <summary>
    /// Whether the given type is suppressible (i.e. NOT transactional). Transactional types are
    /// always delivered and never carry unsubscribe links.
    /// </summary>
    protected static bool IsSuppressible(NotificationType type) =>
        !NotificationCategories.Transactional.Contains(type);

    /// <summary>
    /// Whether the notification is footer-eligible: suppressible type AND a non-null recipient
    /// (null-recipient rows never carry unsubscribe links — the null-recipient invariant).
    /// </summary>
    protected static bool HasFooter(Notification notification) =>
        notification.RecipientId is not null && IsSuppressible(notification.Type);

    /// <summary>
    /// Merges the rendered body with the footer (when present), returning the final plain + html pair.
    /// When the footer is null (transactional or null-recipient) the body is returned unchanged.
    /// </summary>
    protected static (string Plain, string Html) MergeFooter(
        string plain,
        string html,
        EmailFooter? footer)
    {
        if (footer is null)
        {
            return (plain, html);
        }

        return ($"{plain}\n\n{footer.PlainText}", $"{html}{footer.Html}");
    }
}
