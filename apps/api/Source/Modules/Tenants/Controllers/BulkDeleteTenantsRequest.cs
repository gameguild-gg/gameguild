namespace GameGuild.Modules.Tenants;

/// <summary>
/// Request model for bulk deleting tenants
/// </summary>
public record BulkDeleteTenantsRequest(IEnumerable<Guid> TenantIds);