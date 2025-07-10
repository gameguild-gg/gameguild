namespace GameGuild.Modules.Tenants;

/// <summary>
/// Request model for updating a tenant
/// </summary>
public record UpdateTenantRequest(string Name, string? Description = null, bool IsActive = true, string? Slug = null);
