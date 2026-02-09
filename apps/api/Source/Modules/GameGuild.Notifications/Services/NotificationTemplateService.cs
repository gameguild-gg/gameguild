using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Notifications.Services;

/// <summary>
/// Manages notification template CRUD operations and placeholder replacement
/// </summary>
public class NotificationTemplateService(
    IApplicationDbContext context,
    ILogger<NotificationTemplateService> logger) : INotificationTemplateService
{
    public async Task<Result<NotificationTemplate>> GetTemplateByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var template = await context.Set<NotificationTemplate>()
            .FirstOrDefaultAsync(t => t.Code == code, cancellationToken).ConfigureAwait(false);

        if (template == null)
        {
            return Result.Failure<NotificationTemplate>(Error.NotFound("Template.NotFound", $"Template with code '{code}' not found"));
        }

        return Result.Success(template);
    }

    public async Task<Result<IEnumerable<NotificationTemplate>>> GetTemplatesAsync(
        string? category = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<NotificationTemplate>().AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(t => t.Category == category);
        }

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var templates = await query.OrderBy(t => t.Name).ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<NotificationTemplate>>(templates);
    }

    public async Task<Result<NotificationTemplate>> CreateTemplateAsync(
        string code,
        string name,
        NotificationType type,
        NotificationChannel channel,
        string titleTemplate,
        string messageTemplate,
        string? description = null,
        string? actionUrlTemplate = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var existingTemplate = await context.Set<NotificationTemplate>()
            .FirstOrDefaultAsync(t => t.Code == code, cancellationToken).ConfigureAwait(false);

        if (existingTemplate != null)
        {
            return Result.Failure<NotificationTemplate>(Error.Conflict("Template.DuplicateCode", $"Template with code '{code}' already exists"));
        }

        var template = NotificationTemplate.Create(
            code,
            name,
            type,
            channel,
            titleTemplate,
            messageTemplate,
            description,
            actionUrlTemplate,
            null,
            NotificationPriority.Normal,
            null,
            category);

        context.Set<NotificationTemplate>().Add(template);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Notification template created. Id: {TemplateId}, Code: {Code}", template.Id, code);

        return Result.Success(template);
    }

    public async Task<Result<NotificationTemplate>> UpdateTemplateAsync(
        Guid templateId,
        string? titleTemplate = null,
        string? messageTemplate = null,
        string? actionUrlTemplate = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var template = await context.Set<NotificationTemplate>()
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken).ConfigureAwait(false);

        if (template == null)
        {
            return Result.Failure<NotificationTemplate>(Error.NotFound("Template.NotFound", $"Template with ID {templateId} not found"));
        }

        if (titleTemplate != null || messageTemplate != null || actionUrlTemplate != null)
        {
            template.UpdateContent(
                titleTemplate ?? template.TitleTemplate,
                messageTemplate ?? template.MessageTemplate,
                actionUrlTemplate ?? template.ActionUrlTemplate,
                template.DefaultIconUrl);
        }

        if (isActive.HasValue)
        {
            if (isActive.Value)
            {
                template.Activate();
            }
            else
            {
                template.Deactivate();
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(template);
    }

    public string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        var result = template;
        foreach (var placeholder in placeholders)
        {
            result = result.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
        }
        return result;
    }
}
