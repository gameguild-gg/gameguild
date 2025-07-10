namespace GameGuild.Modules.Tenants;

/// <summary>
/// Request model for creating a tenant
/// </summary>
public record CreateTenantRequest(string Name, string? Description = null, bool IsActive = true, string? Slug = null);