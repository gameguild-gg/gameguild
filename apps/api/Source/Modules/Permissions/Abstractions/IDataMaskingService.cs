using GameGuild.Modules.Permissions.Entities;

namespace GameGuild.Modules.Permissions.Abstractions;

/// <summary>
/// Service for managing and applying data masking rules
/// </summary>
public interface IDataMaskingService
{
    /// <summary>
    /// Applies masking rules to a field value
    /// </summary>
    Task<Result<MaskedFieldResult>> ApplyMaskingAsync(
        Guid userId,
        Guid? tenantId,
        string resourceType,
        string fieldName,
        object? fieldValue,
        string? resourceId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies masking rules to multiple fields in an object
    /// </summary>
    Task<Result<Dictionary<string, object?>>> ApplyMaskingToObjectAsync<T>(
        Guid userId,
        Guid? tenantId,
        string resourceType,
        T obj,
        string? resourceId = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Checks if a user can see unmasked data for a specific field
    /// </summary>
    Task<Result<bool>> CanSeeUnmaskedAsync(
        Guid userId,
        Guid? tenantId,
        string resourceType,
        string fieldName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new data masking rule
    /// </summary>
    Task<Result<DataMaskingRule>> CreateRuleAsync(
        DataMaskingRule rule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing data masking rule
    /// </summary>
    Task<Result<DataMaskingRule>> UpdateRuleAsync(
        DataMaskingRule rule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a data masking rule
    /// </summary>
    Task<Result> DeleteRuleAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a data masking rule by ID
    /// </summary>
    Task<Result<DataMaskingRule>> GetRuleAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all masking rules for a tenant
    /// </summary>
    Task<Result<List<DataMaskingRule>>> ListRulesAsync(
        Guid? tenantId,
        string? resourceType = null,
        bool includeDisabled = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets data access logs for auditing
    /// </summary>
    Task<Result<List<DataAccessLog>>> GetAccessLogsAsync(
        Guid? tenantId,
        Guid? userId = null,
        string? resourceType = null,
        bool? wasMasked = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about masking rule usage
    /// </summary>
    Task<Result<MaskingStatistics>> GetStatisticsAsync(
        Guid? tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of applying masking to a field
/// </summary>
public class MaskedFieldResult
{
    public object? MaskedValue { get; set; }
    public bool WasMasked { get; set; }
    public string? RuleName { get; set; }
    public Guid? RuleId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Statistics about data masking
/// </summary>
public class MaskingStatistics
{
    public int TotalRules { get; set; }
    public int EnabledRules { get; set; }
    public long TotalAccesses { get; set; }
    public long MaskedAccesses { get; set; }
    public long UnmaskedAccesses { get; set; }
    public Dictionary<string, int> AccessesByResourceType { get; set; } = new();
    public Dictionary<string, int> AccessesByField { get; set; } = new();
    public Dictionary<MaskingType, int> RulesByMaskingType { get; set; } = new();
}
