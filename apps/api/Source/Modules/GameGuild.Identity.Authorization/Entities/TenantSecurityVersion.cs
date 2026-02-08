using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Stores security version numbers for cache invalidation per tenant.
/// </summary>
[Table("TenantSecurityVersions")]
[Index(nameof(TenantId), IsUnique = true)]
public class TenantSecurityVersion : EntityBase
{
    /// <summary>
    ///     Gets or sets the tenant ID.
    /// </summary>
    [Required]
    public new Guid TenantId { get; set; }

    /// <summary>
    ///     Gets or sets the current security version number.
    ///     Incremented when security-related changes occur (Access Control List updates, policy changes, etc.).
    /// </summary>
    public long SecurityVersion { get; set; } = 1;

    /// <summary>
    ///     Gets or sets when the version was last updated.
    /// </summary>
    [Required]
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets a description of the last change that caused the version increment.
    /// </summary>
    [MaxLength(500)]
    public string? LastChangeReason { get; set; }

    /// <summary>
    ///     Increments the security version.
    /// </summary>
    /// <param name="reason">Optional reason for the increment.</param>
    /// <returns>The new version number.</returns>
    public long IncrementVersion(string? reason = null)
    {
        SecurityVersion++;
        LastUpdatedAt = DateTime.UtcNow;
        LastChangeReason = reason;
        Touch();
        return SecurityVersion;
    }
}
