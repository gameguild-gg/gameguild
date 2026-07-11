using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.CQRS.Models;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Notifications;

/// <summary>
/// Represents a notification template for generating consistent notifications
/// </summary>
[Table("NotificationTemplates")]
[Index(nameof(Code), IsUnique = true)]
[Index(nameof(Type))]
[Index(nameof(Channel))]
[Index(nameof(IsActive))]
public class NotificationTemplate : EntityBase
{
    /// <summary>
    /// Unique code identifier for this template
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Human-readable name for the template
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Description of when this template should be used
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; private set; }

    /// <summary>
    /// The type of notification this template produces
    /// </summary>
    public NotificationType Type { get; private set; }

    /// <summary>
    /// The default delivery channel for this template
    /// </summary>
    public NotificationChannel Channel { get; private set; }

    /// <summary>
    /// Title template with placeholder support (e.g., "Welcome, {{userName}}!")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string TitleTemplate { get; private set; } = string.Empty;

    /// <summary>
    /// Message body template with placeholder support
    /// </summary>
    [Required]
    [MaxLength(4000)]
    public string MessageTemplate { get; private set; } = string.Empty;

    /// <summary>
    /// Optional action URL template
    /// </summary>
    [MaxLength(500)]
    public string? ActionUrlTemplate { get; private set; }

    /// <summary>
    /// Optional default icon URL
    /// </summary>
    [MaxLength(500)]
    public string? DefaultIconUrl { get; private set; }

    /// <summary>
    /// Default priority for notifications created from this template
    /// </summary>
    public NotificationPriority DefaultPriority { get; private set; } = NotificationPriority.Normal;

    /// <summary>
    /// Whether this template is active and can be used
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Tenant ID if this is a tenant-specific template
    /// </summary>
    [NotMapped]
    public TenantId? TemplateTenantId => TenantId.HasValue ? new TenantId(TenantId.Value) : null;

    /// <summary>
    /// Category for grouping templates (e.g., "Learning", "Social", "Billing")
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; private set; }

    /// <summary>
    /// JSON array of supported placeholder names for validation
    /// </summary>
    [MaxLength(1000)]
    public string? SupportedPlaceholders { get; private set; }

    /// <summary>
    /// EF Core constructor
    /// </summary>
    private NotificationTemplate() { }

    /// <summary>
    /// Creates a new notification template
    /// </summary>
    public static NotificationTemplate Create(
        string code,
        string name,
        NotificationType type,
        NotificationChannel channel,
        string titleTemplate,
        string messageTemplate,
        string? description = null,
        string? actionUrlTemplate = null,
        string? defaultIconUrl = null,
        NotificationPriority defaultPriority = NotificationPriority.Normal,
        Guid? tenantId = null,
        string? category = null,
        string? supportedPlaceholders = null)
    {
        return new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Type = type,
            Channel = channel,
            TitleTemplate = titleTemplate,
            MessageTemplate = messageTemplate,
            Description = description,
            ActionUrlTemplate = actionUrlTemplate,
            DefaultIconUrl = defaultIconUrl,
            DefaultPriority = defaultPriority,
            TenantId = tenantId,
            Category = category,
            SupportedPlaceholders = supportedPlaceholders,
            IsActive = true
        };
    }

    /// <summary>
    /// Updates the template content
    /// </summary>
    public void UpdateContent(
        string titleTemplate,
        string messageTemplate,
        string? actionUrlTemplate = null,
        string? defaultIconUrl = null)
    {
        TitleTemplate = titleTemplate;
        MessageTemplate = messageTemplate;
        ActionUrlTemplate = actionUrlTemplate;
        DefaultIconUrl = defaultIconUrl;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Updates template metadata
    /// </summary>
    public void UpdateMetadata(
        string name,
        string? description,
        string? category,
        NotificationPriority defaultPriority)
    {
        Name = name;
        Description = description;
        Category = category;
        DefaultPriority = defaultPriority;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Activates the template
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Deactivates the template
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Soft deletes the template
    /// </summary>
    public void Delete() => SoftDelete();
}
