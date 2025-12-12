namespace GameGuild.Tenants.Commands;

/// <summary>
///     Response model for created tenant
/// </summary>
/// <param name="TenantId">Created tenant ID</param>
/// <param name="Name">Tenant name</param>
/// <param name="Slug">Tenant slug</param>
public record CreateTenantResponse(Guid TenantId, string Name, string Slug);
