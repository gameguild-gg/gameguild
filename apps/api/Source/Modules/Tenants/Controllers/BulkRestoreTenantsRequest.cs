namespace GameGuild.Modules.Tenants;

/// <summary>
/// Request model for bulk restoring tenants
/// </summary>
public record BulkRestoreTenantsRequest(IEnumerable<Guid> TenantIds);