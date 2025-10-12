using GameGuild.Modules.Permissions.Entities;

namespace GameGuild.Modules.Permissions.Abstractions;

/// <summary>
/// Service for managing permission templates
/// </summary>
public interface IPermissionTemplateService
{
    /// <summary>
    /// Get all available templates
    /// </summary>
    Task<IEnumerable<PermissionTemplate>> GetTemplatesAsync(string? category = null, bool includeInactive = false);

    /// <summary>
    /// Get template by name
    /// </summary>
    Task<PermissionTemplate?> GetTemplateByNameAsync(string name);

    /// <summary>
    /// Create a new custom template
    /// </summary>
    Task<PermissionTemplate> CreateTemplateAsync(
        string name,
        string description,
        PermissionType[] permissions,
        ModuleType? module = null,
        string? category = null);

    /// <summary>
    /// Update an existing template (only custom templates)
    /// </summary>
    Task<PermissionTemplate> UpdateTemplateAsync(
        Guid templateId,
        string? name = null,
        string? description = null,
        PermissionType[]? permissions = null,
        string? category = null);

    /// <summary>
    /// Delete a custom template
    /// </summary>
    Task DeleteTemplateAsync(Guid templateId);

    /// <summary>
    /// Apply a template to a user in a tenant
    /// </summary>
    Task ApplyTemplateToUserAsync(string templateName, Guid userId, Guid tenantId);

    /// <summary>
    /// Apply a template to multiple users
    /// </summary>
    Task ApplyTemplateToUsersAsync(string templateName, Guid[] userIds, Guid tenantId);

    /// <summary>
    /// Set template as tenant default
    /// </summary>
    Task SetTenantDefaultTemplateAsync(Guid tenantId, string templateName);

    /// <summary>
    /// Get system templates
    /// </summary>
    Task<IEnumerable<PermissionTemplate>> GetSystemTemplatesAsync();

    /// <summary>
    /// Initialize system templates if they don't exist
    /// </summary>
    Task InitializeSystemTemplatesAsync();
}