using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Entities;
using GameGuild.Modules.Permissions.Abstractions;

namespace GameGuild.Modules.Permissions.Commands;

// Create Rule Command
public record CreateSoDRuleCommand(
    Guid? TenantId,
    string Name,
    string? Description,
    SoDRuleType RuleType,
    SoDSeverity Severity,
    string ConflictingPermissions,
    string? ConflictingRoles,
    string? ConflictingResources,
    bool RequireApproval,
    string? ApproverRoles,
    Guid CreatedBy
) : IRequest<Result<SoDRule>>;

// Update Rule Command
public record UpdateSoDRuleCommand(
    Guid RuleId,
    string Name,
    string? Description,
    SoDRuleType RuleType,
    SoDSeverity Severity,
    bool IsEnabled,
    string ConflictingPermissions,
    string? ConflictingRoles,
    string? ConflictingResources,
    bool RequireApproval,
    string? ApproverRoles
) : IRequest<Result<SoDRule>>;

// Delete Rule Command
public record DeleteSoDRuleCommand(Guid RuleId) : IRequest<Result>;

// Get Rule Query
public record GetSoDRuleQuery(Guid RuleId) : IRequest<Result<SoDRule>>;

// List Rules Query
public record ListSoDRulesQuery(Guid? TenantId) : IRequest<Result<List<SoDRule>>>;

// Detect Violations Command
public record DetectSoDViolationsCommand(Guid UserId, Guid? TenantId) : IRequest<Result<List<SoDViolation>>>;

// Resolve Violation Command
public record ResolveSoDViolationCommand(
    Guid ViolationId,
    SoDResolutionAction Action,
    string? Notes,
    Guid ResolvedBy
) : IRequest<Result<SoDViolation>>;

// Get Active Violations Query
public record GetActiveSoDViolationsQuery(Guid? TenantId) : IRequest<Result<List<SoDViolation>>>;

// Scan All Users Command
public record ScanAllUsersForSoDCommand(Guid? TenantId) : IRequest<Result>;

// Get Statistics Query
public record GetSoDStatisticsQuery(Guid? TenantId) : IRequest<Result<SoDStatistics>>;
