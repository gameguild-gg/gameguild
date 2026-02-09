namespace GameGuild.Identity.Tenants;

/// <summary>
///     Request model for creating a tenant
/// </summary>
/// <param name="Name">Tenant name</param>
/// <param name="Slug">Tenant slug (unique identifier)</param>
/// <param name="AdminEmail">Administrator email address</param>
/// <param name="Description">Optional tenant description</param>
public sealed record CreateTenantRequest(string Name, string Slug, string AdminEmail, string? Description = null);
