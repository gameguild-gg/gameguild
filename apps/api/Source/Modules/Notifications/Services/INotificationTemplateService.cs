using System.Text.RegularExpressions;


namespace GameGuild.Modules.Notifications;

/// <summary>
/// Service for managing notification templates.
/// </summary>
public interface INotificationTemplateService
{
    Task<NotificationTemplate?> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default);
    Task<RenderedNotification> RenderTemplateAsync(NotificationTemplate template, Dictionary<string, string> variables, CancellationToken cancellationToken = default);
}

/// <summary>
/// Rendered notification from template.
/// </summary>
public sealed class RenderedNotification
{
    public required string Title { get; init; }
    public required string Content { get; init; }
    public string? EmailSubject { get; init; }
    public string? EmailBody { get; init; }
    public string? PushTitle { get; init; }
    public string? PushBody { get; init; }
    public string? SmsMessage { get; init; }
}

/// <summary>
/// Implementation of notification template service.
/// </summary>
public sealed class NotificationTemplateService : INotificationTemplateService
{
    private readonly Dictionary<string, NotificationTemplate> _templates = new();

    public Task<NotificationTemplate?> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default)
    {
        _templates.TryGetValue(templateId, out var template);
        return Task.FromResult(template);
    }

    public Task<RenderedNotification> RenderTemplateAsync(
        NotificationTemplate template,
        Dictionary<string, string> variables,
        CancellationToken cancellationToken = default)
    {
        var rendered = new RenderedNotification
        {
            Title = RenderString(template.TitleTemplate, variables),
            Content = RenderString(template.ContentTemplate, variables),
            EmailSubject = template.EmailSubjectTemplate != null ? RenderString(template.EmailSubjectTemplate, variables) : null,
            EmailBody = template.EmailBodyTemplate != null ? RenderString(template.EmailBodyTemplate, variables) : null,
            PushTitle = template.PushTitleTemplate != null ? RenderString(template.PushTitleTemplate, variables) : null,
            PushBody = template.PushBodyTemplate != null ? RenderString(template.PushBodyTemplate, variables) : null,
            SmsMessage = template.SmsTemplate != null ? RenderString(template.SmsTemplate, variables) : null
        };

        return Task.FromResult(rendered);
    }

    private string RenderString(string template, Dictionary<string, string> variables)
    {
        return Regex.Replace(template, @"\{\{(\w+)\}\}", match =>
        {
            var key = match.Groups[1].Value;
            return variables.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}
