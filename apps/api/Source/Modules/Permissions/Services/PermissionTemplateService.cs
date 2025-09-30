using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Permissions.Services;

/// <summary>
/// Service for managing permission templates
/// </summary>
public class PermissionTemplateService : IPermissionTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionTemplateService> _logger;

    public PermissionTemplateService(
        ApplicationDbContext context,
        IPermissionService permissionService,
        ILogger<PermissionTemplateService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<PermissionTemplate>> GetTemplatesAsync(string? category = null, bool includeInactive = false)
    {
        var query = _context.PermissionTemplates.AsQueryable();

        if (!includeInactive)
            query = query.Where(t => t.IsActive);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category == category);

        return await query
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<PermissionTemplate?> GetTemplateByNameAsync(string name)
    {
        return await _context.PermissionTemplates
            .FirstOrDefaultAsync(t => t.Name == name && t.IsActive);
    }

    public async Task<PermissionTemplate> CreateTemplateAsync(
        string name,
        string description,
        PermissionType[] permissions,
        ModuleType? module = null,
        string? category = null)
    {
        // Check if template name already exists
        var existingTemplate = await GetTemplateByNameAsync(name);
        if (existingTemplate != null)
            throw new InvalidOperationException($"Template with name '{name}' already exists");

        var template = new PermissionTemplate
        {
            Name = name,
            Description = description,
            Permissions = permissions,
            Module = module,
            Category = category ?? "Custom",
            IsSystemTemplate = false,
            IsActive = true
        };

        _context.PermissionTemplates.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created custom permission template: {TemplateName}", name);

        return template;
    }

    public async Task<PermissionTemplate> UpdateTemplateAsync(
        Guid templateId,
        string? name = null,
        string? description = null,
        PermissionType[]? permissions = null,
        string? category = null)
    {
        var template = await _context.PermissionTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
            throw new ArgumentException($"Template with ID {templateId} not found");

        if (template.IsSystemTemplate)
            throw new InvalidOperationException("Cannot modify system templates");

        if (!string.IsNullOrEmpty(name) && name != template.Name)
        {
            // Check if new name conflicts with existing template
            var existingTemplate = await GetTemplateByNameAsync(name);
            if (existingTemplate != null && existingTemplate.Id != templateId)
                throw new InvalidOperationException($"Template with name '{name}' already exists");

            template.Name = name;
        }

        if (!string.IsNullOrEmpty(description))
            template.Description = description;

        if (permissions != null)
            template.Permissions = permissions;

        if (!string.IsNullOrEmpty(category))
            template.Category = category;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated permission template: {TemplateName}", template.Name);

        return template;
    }

    public async Task DeleteTemplateAsync(Guid templateId)
    {
        var template = await _context.PermissionTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
            throw new ArgumentException($"Template with ID {templateId} not found");

        if (template.IsSystemTemplate)
            throw new InvalidOperationException("Cannot delete system templates");

        template.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted permission template: {TemplateName}", template.Name);
    }

    public async Task ApplyTemplateToUserAsync(string templateName, Guid userId, Guid tenantId)
    {
        var template = await GetTemplateByNameAsync(templateName);
        if (template == null)
            throw new ArgumentException($"Template '{templateName}' not found");

        await _permissionService.GrantTenantPermissionAsync(userId, tenantId, template.Permissions);

        _logger.LogInformation("Applied template {TemplateName} to User:{UserId} in Tenant:{TenantId}",
            templateName, userId, tenantId);
    }

    public async Task ApplyTemplateToUsersAsync(string templateName, Guid[] userIds, Guid tenantId)
    {
        var template = await GetTemplateByNameAsync(templateName);
        if (template == null)
            throw new ArgumentException($"Template '{templateName}' not found");

        await _permissionService.BulkGrantTenantPermissionAsync(userIds, tenantId, template.Permissions);

        _logger.LogInformation("Applied template {TemplateName} to {UserCount} users in Tenant:{TenantId}",
            templateName, userIds.Length, tenantId);
    }

    public async Task SetTenantDefaultTemplateAsync(Guid tenantId, string templateName)
    {
        var template = await GetTemplateByNameAsync(templateName);
        if (template == null)
            throw new ArgumentException($"Template '{templateName}' not found");

        await _permissionService.SetTenantDefaultPermissionsAsync(tenantId, template.Permissions);

        _logger.LogInformation("Set default template {TemplateName} for Tenant:{TenantId}",
            templateName, tenantId);
    }

    public async Task<IEnumerable<PermissionTemplate>> GetSystemTemplatesAsync()
    {
        return await _context.PermissionTemplates
            .Where(t => t.IsSystemTemplate && t.IsActive)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task InitializeSystemTemplatesAsync()
    {
        var systemTemplates = PermissionTemplate.SystemTemplates.GetAll();

        foreach (var template in systemTemplates)
        {
            var existingTemplate = await _context.PermissionTemplates
                .FirstOrDefaultAsync(t => t.Name == template.Name && t.IsSystemTemplate);

            if (existingTemplate == null)
            {
                _context.PermissionTemplates.Add(template);
                _logger.LogInformation("Initialized system template: {TemplateName}", template.Name);
            }
            else
            {
                // Update existing system template if needed
                existingTemplate.Description = template.Description;
                existingTemplate.Permissions = template.Permissions;
                existingTemplate.Category = template.Category;
                existingTemplate.IsActive = template.IsActive;
            }
        }

        await _context.SaveChangesAsync();
    }
}