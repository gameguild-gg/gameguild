using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Entities;
using GameGuild.Shared;

namespace GameGuild.Modules.Permissions.Commands;

// Create Command
public record CreateDataMaskingRuleCommand(
    string Name,
    string Description,
    Guid TenantId,
    string ResourceType,
    string FieldName,
    MaskingType MaskingType,
    string? MaskingPattern,
    List<Guid>? ExemptUserIds,
    List<Guid>? ExemptRoleIds,
    List<string>? ExemptPermissions,
    bool IsEnabled
) : IRequest<Result<DataMaskingRule>>;

// Update Command
public record UpdateDataMaskingRuleCommand(
    Guid RuleId,
    string Name,
    string Description,
    string ResourceType,
    string FieldName,
    MaskingType MaskingType,
    string? MaskingPattern,
    List<Guid>? ExemptUserIds,
    List<Guid>? ExemptRoleIds,
    List<string>? ExemptPermissions,
    bool IsEnabled
) : IRequest<Result<DataMaskingRule>>;

// Delete Command
public record DeleteDataMaskingRuleCommand(Guid RuleId) : IRequest<Result>;

// Apply Masking Query
public record ApplyDataMaskingQuery(
    string ResourceType,
    string FieldName,
    string Value,
    Guid UserId,
    Guid TenantId
) : IRequest<Result<MaskedFieldResult>>;

// Check Exemption Query
public record CheckMaskingExemptionQuery(
    Guid RuleId,
    Guid UserId
) : IRequest<Result<bool>>;

// Get Rule Query
public record GetDataMaskingRuleQuery(Guid RuleId) : IRequest<Result<DataMaskingRule>>;

// List Rules Query
public record ListDataMaskingRulesQuery(Guid? TenantId) : IRequest<Result<List<DataMaskingRule>>>;

// Get Access Logs Query
public record GetDataAccessLogsQuery(
    Guid? RuleId,
    Guid? UserId,
    DateTime? FromDate,
    DateTime? ToDate
) : IRequest<Result<List<DataAccessLog>>>;

// Get Statistics Query
public record GetMaskingStatisticsQuery(Guid RuleId) : IRequest<Result<MaskingStatistics>>;
