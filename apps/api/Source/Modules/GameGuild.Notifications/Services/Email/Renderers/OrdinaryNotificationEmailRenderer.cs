using System.Net;
using GameGuild.Email;

namespace GameGuild.Notifications.Services.Email.Renderers;

/// <summary>
///     Generic renderer for ORDINARY (non-transactional) notification types that have no
///     dedicated renderer (issue #1319 N2: 28 of 43 types were renderer-less, so any Email-channel
///     row of those types was a guaranteed dead-letter "No email renderer registered"). Renders
///     the row's <see cref="Notification.Title"/>/<see cref="Notification.Message"/> with an
///     optional action link — the channel-agnostic payload every producer already provides.
/// </summary>
/// <remarks>
///     <para>
///     One instance is registered per covered type (the registry keys renderers by
///     <see cref="IEmailRenderer.Type"/> and enforces one renderer per type). Dedicated renderers
///     keep precedence: this family only covers types nothing else renders.
///     </para>
///     <para>
///     <b>Deliberately NOT covered:</b> <see cref="NotificationType.SmsMfaCode"/> and
///     <see cref="NotificationType.PushMfaChallenge"/> are transactional MFA codes native to other
///     channels; an Email row of those types signals a producer bug and still dead-letters rather
///     than silently emailing an MFA payload.
///     </para>
/// </remarks>
public sealed class OrdinaryNotificationEmailRenderer(NotificationType type, IEmailFooterService footerService)
    : EmailRendererBase, IEmailRenderer
{
    /// <summary>
    ///     The ordinary notification types covered by this renderer family — every enum member
    ///     that is neither transactional (<see cref="NotificationCategories.Transactional"/>)
    ///     nor already covered by a dedicated renderer elsewhere in the platform.
    /// </summary>
    public static readonly IReadOnlyList<NotificationType> CoveredTypes =
    [
        NotificationType.System,
        NotificationType.CourseEnrollment,
        NotificationType.CourseCompletion,
        NotificationType.AchievementUnlocked,
        NotificationType.CertificateIssued,
        NotificationType.NewContent,
        NotificationType.AssessmentReminder,
        NotificationType.AssessmentGraded,
        NotificationType.CohortActivity,
        NotificationType.SocialInteraction,
        NotificationType.DirectMessage,
        NotificationType.Marketing,
        NotificationType.FeatureAnnouncement,
        NotificationType.Recommendation,
        NotificationType.ProgressMilestone,
        NotificationType.InactivityReminder,
        NotificationType.PaymentMethodExpiring,
        NotificationType.WalletBalanceBelowThreshold,
        NotificationType.CreditApplied,
        NotificationType.PaymentReceipt,
        NotificationType.RefundCompleted,
        NotificationType.PaymentMethodUpdateRequired,
        NotificationType.DisputeCreated,
        NotificationType.DisputeDeadlineReminder,
        NotificationType.LegalHoldNotice,
        NotificationType.Custom
    ];

    private readonly IEmailFooterService _footerService = footerService ?? throw new ArgumentNullException(nameof(footerService));

    public NotificationType Type { get; } = type;

    public Task<EmailMessage?> RenderAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var recipientName = notification.RecipientEmail?.Trim() ?? string.Empty;
        var title = string.IsNullOrWhiteSpace(notification.Title) ? "GameGuild notification" : notification.Title.Trim();
        var message = notification.Message?.Trim() ?? string.Empty;
        var actionUrl = notification.ActionUrl?.Trim();

        var plain = string.IsNullOrWhiteSpace(actionUrl)
            ? $"{title}\n\n{message}"
            : $"{title}\n\n{message}\n\nOpen in GameGuild: {actionUrl}";
        var html = string.IsNullOrWhiteSpace(actionUrl)
            ? $"<h2>{WebUtility.HtmlEncode(title)}</h2><p>{WebUtility.HtmlEncode(message)}</p>"
            : $"<h2>{WebUtility.HtmlEncode(title)}</h2><p>{WebUtility.HtmlEncode(message)}</p><p><a href=\"{WebUtility.HtmlEncode(actionUrl)}\">Open in GameGuild</a></p>";

        var (finalPlain, finalHtml) = MergeFooter(plain, html, _footerService.Build(notification));
        var emailMessage = new EmailMessage(
            notification.RecipientEmail ?? string.Empty,
            title,
            finalPlain,
            finalHtml,
            recipientName);
        return Task.FromResult<EmailMessage?>(emailMessage);
    }
}
