using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Permissions.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GameGuild.Permissions.Application.Services;

/// <summary>
///     Service for managing permission templates
/// </summary>
public class PermissionTemplateService(IPermissionTemplateRepository repository, IPermissionService permissionService, ILogger<PermissionTemplateService> logger) : IPermissionTemplateService
{
    private readonly ILogger<PermissionTemplateService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPermissionService _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));

    private readonly IPermissionTemplateRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<PermissionTemplate> CreateTemplateAsync(PermissionTemplate template, CancellationToken cancellationToken = default)
    {
        if (template is null) throw new ArgumentNullException(nameof(template));

        _logger.LogInformation("Creating permission template: {TemplateName}", template.Name);

        return await _repository.CreateAsync(template, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PermissionTemplate> UpdateTemplateAsync(PermissionTemplate template, CancellationToken cancellationToken = default)
    {
        if (template is null) throw new ArgumentNullException(nameof(template));

        // Prevent modification of system templates
        var existing = await _repository.GetByIdAsync(template.Id, cancellationToken).ConfigureAwait(false);

        if (existing?.IsSystemTemplate == true) { throw new InvalidOperationException("Cannot modify system templates"); }

        _logger.LogInformation("Updating permission template: {TemplateId}", template.Id);

        return await _repository.UpdateAsync(template, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(templateId, cancellationToken).ConfigureAwait(false);

        if (existing?.IsSystemTemplate == true) { throw new InvalidOperationException("Cannot delete system templates"); }

        _logger.LogInformation("Deleting permission template: {TemplateId}", templateId);

        return await _repository.DeleteAsync(templateId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PermissionTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default) { return await _repository.GetByIdAsync(templateId, cancellationToken); }

    public async Task<PermissionTemplate?> GetTemplateByNameAsync(string name, CancellationToken cancellationToken = default) { return await _repository.GetByNameAsync(name, cancellationToken); }

    public async Task<List<PermissionTemplate>> GetTemplatesAsync(string? category = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(category)) { return await _repository.GetByCategoryAsync(category, includeInactive, cancellationToken); }

        return await _repository.GetAllAsync(includeInactive, cancellationToken);
    }

    public async Task<List<PermissionTemplate>> GetSystemTemplatesAsync(CancellationToken cancellationToken = default) { return await _repository.GetSystemTemplatesAsync(cancellationToken); }

    public async Task<TenantPermission> ApplyTemplateToUserAsync(Guid templateId, Guid userId, Guid tenantId, Guid? appliedBy = null, CancellationToken cancellationToken = default)
    {
        var template = await _repository.GetByIdAsync(templateId, cancellationToken);

        if (template == null) { throw new InvalidOperationException($"Template {templateId} not found"); }

        if (!template.IsActive) { throw new InvalidOperationException($"Template {template.Name} is not active"); }

        _logger.LogInformation("Applying template {TemplateName} to user {UserId} in tenant {TenantId}", template.Name, userId, tenantId);

        return await _permissionService.GrantTenantPermissionAsync(userId, tenantId, template.Permissions, appliedBy, null, $"Applied template: {template.Name}", cancellationToken);
    }

    public async Task<int> BulkApplyTemplateAsync(Guid templateId, Guid[ ] userIds, Guid tenantId, Guid? appliedBy = null, CancellationToken cancellationToken = default)
    {
        if (userIds is null) throw new ArgumentNullException(nameof(userIds));

        var template = await _repository.GetByIdAsync(templateId, cancellationToken);

        if (template == null) { throw new InvalidOperationException($"Template {templateId} not found"); }

        if (!template.IsActive) { throw new InvalidOperationException($"Template {template.Name} is not active"); }

        _logger.LogInformation("Bulk applying template {TemplateName} to {UserCount} users", template.Name, userIds.Length);

        var count = 0;

        foreach (var userId in userIds)
        {
            await _permissionService.GrantTenantPermissionAsync(userId, tenantId, template.Permissions, appliedBy, null, $"Applied template: {template.Name}", cancellationToken);
            count++;
        }

        return count;
    }

    public async Task<List<string>> GetTemplatePermissionsAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _repository.GetByIdAsync(templateId, cancellationToken);

        return template?.Permissions.ToList() ?? new List<string>();
    }
}
