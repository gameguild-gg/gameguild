using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Request DTO for archiving a tenant
/// </summary>
public class ArchiveTenantRequest
{
    /// <summary>
    ///     Optional reason for archiving the tenant
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
}