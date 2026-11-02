using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     One entry in the tenant-scoped fraud blacklist / manual whitelist override
///     (issue #409). Patterns may contain <c>*</c> wildcards, entries may expire, and a
///     whitelist entry (IsWhitelist) suppresses blacklist matches for the same value.
/// </summary>
[Table("FraudBlacklistEntries")]
public class FraudBlacklistEntry : EntityBase
{
    /// <summary>Tenant the entry belongs to.</summary>
    [Required]
    public new Guid TenantId
    {
        get => base.TenantId ?? Guid.Empty;
        set => base.TenantId = value;
    }

    /// <summary>Kind of value being listed.</summary>
    [Required]
    [MaxLength(32)]
    public FraudBlacklistEntryType EntryType { get; private set; }

    /// <summary>Normalized pattern; may contain <c>*</c> wildcards.</summary>
    [Required]
    [MaxLength(255)]
    public string Value { get; private set; } = string.Empty;

    /// <summary>Bounded reason recorded when the entry was added.</summary>
    [MaxLength(1000)]
    public string? Reason { get; private set; }

    /// <summary>
    ///     When true this entry is a manual whitelist override: matching values are never
    ///     treated as blacklisted (issue #409: manual whitelist override capability).
    /// </summary>
    public bool IsWhitelist { get; private set; }

    /// <summary>How the entry was added.</summary>
    [Required]
    [MaxLength(32)]
    public FraudBlacklistSource Source { get; private set; }

    /// <summary>When the entry stops matching; null = no expiry (issue #409: blacklist with expiry dates).</summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>Administrator who added the entry, when added manually.</summary>
    public Guid? CreatedByUserId { get; private set; }

    /// <summary>Whether the entry has expired at the given instant.</summary>
    /// <param name="nowUtc">Current UTC instant.</param>
    /// <returns>True when <see cref="ExpiresAt"/> is in the past.</returns>
    public bool IsExpired(DateTime nowUtc) => ExpiresAt.HasValue && ExpiresAt.Value <= nowUtc;

    /// <summary>Creates a blacklist or whitelist-override entry with a normalized value.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="entryType">Entry kind.</param>
    /// <param name="value">Raw pattern; normalized (trimmed, lowercased) on store.</param>
    /// <param name="reason">Bounded reason.</param>
    /// <param name="isWhitelist">Whether this is a whitelist override.</param>
    /// <param name="source">How the entry is being added.</param>
    /// <param name="expiresAt">Optional expiry instant.</param>
    /// <param name="createdByUserId">Administrator adding the entry, when manual.</param>
    /// <returns>The new entry, or null when <paramref name="value"/> is blank.</returns>
    public static FraudBlacklistEntry? Create(
        Guid tenantId,
        FraudBlacklistEntryType entryType,
        string value,
        string? reason,
        bool isWhitelist,
        FraudBlacklistSource source,
        DateTime? expiresAt = null,
        Guid? createdByUserId = null)
    {
        var normalized = FraudBlacklistPatterns.Normalize(value);
        if (tenantId == Guid.Empty || normalized is null || normalized.Length > 255)
        {
            return null;
        }

        return new FraudBlacklistEntry
        {
            TenantId = tenantId,
            EntryType = entryType,
            Value = normalized,
            Reason = reason is null ? null : (reason.Length <= 1000 ? reason : reason[..1000]),
            IsWhitelist = isWhitelist,
            Source = source,
            ExpiresAt = expiresAt,
            CreatedByUserId = createdByUserId
        };
    }
}
