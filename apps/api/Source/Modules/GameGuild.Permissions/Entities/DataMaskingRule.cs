using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Domain.Entities;

/// <summary>
///     Represents a data masking rule enforced by the authorization layer
/// </summary>
public class DataMaskingRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public TenantId? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ResourceType { get; set; } = string.Empty;

    public string FieldName { get; set; } = string.Empty;

    public MaskingType MaskingType { get; set; }

    public string? MaskingPattern { get; set; }

    public int? ShowFirst { get; set; }

    public int? ShowLast { get; set; }

    public char MaskCharacter { get; set; } = '*';

    // Stored as JSON arrays
    public string? ExemptRoles { get; set; }

    public string? RequiredPermissions { get; set; }

    public string? ExemptUsers { get; set; }

    public bool IsEnabled { get; set; } = true;

    public int Priority { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Guid CreatedBy { get; set; }

    /// <summary>
    ///     Apply masking to a field value
    /// </summary>
    public string ApplyMasking(string value)
    {
        if (!IsEnabled || string.IsNullOrEmpty(value)) return value;

        return MaskingType switch
        {
            MaskingType.Full => new string(MaskCharacter, value.Length),
            MaskingType.Partial => ApplyPartialMask(value),
            MaskingType.Hash => $"#{value.GetHashCode():X8}",
            MaskingType.PatternMask => ApplyPatternMask(value),
            MaskingType.Redact => "[REDACTED]",
            _ => value
        };
    }

    private string ApplyPartialMask(string value)
    {
        var showFirstCount = ShowFirst ?? 0;
        var showLastCount = ShowLast ?? 0;

        if (showFirstCount + showLastCount >= value.Length) return value;

        var maskLength = value.Length - showFirstCount - showLastCount;
        var maskedPart = new string(MaskCharacter, maskLength);

        return value.Substring(0, showFirstCount) + maskedPart + value.Substring(value.Length - showLastCount);
    }

    private string ApplyPatternMask(string value)
    {
        if (string.IsNullOrEmpty(MaskingPattern)) return new string(MaskCharacter, value.Length);

        // Simple pattern replacement logic
        // TODO: Implement more sophisticated pattern matching if needed
        return MaskingPattern;
    }

    /// <summary>
    ///     Check if user is exempt from masking
    /// </summary>
    public bool IsUserExempt(Guid userId)
    {
        if (string.IsNullOrEmpty(ExemptUsers)) return false;

        // TODO: Parse JSON array and check if userId is in the list
        return ExemptUsers.Contains(userId.ToString());
    }

    /// <summary>
    ///     Enable the rule
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Disable the rule
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
