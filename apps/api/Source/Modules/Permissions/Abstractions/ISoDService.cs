using GameGuild.Modules.Permissions.Entities;
using GameGuild.Shared;

namespace GameGuild.Modules.Permissions.Abstractions;

public interface ISoDService
{
    Task<Result<SoDRule>> CreateRuleAsync(SoDRule rule, CancellationToken cancellationToken = default);
    Task<Result<SoDRule>> UpdateRuleAsync(SoDRule rule, CancellationToken cancellationToken = default);
    Task<Result> DeleteRuleAsync(Guid ruleId, CancellationToken cancellationToken = default);
    Task<Result<SoDRule>> GetRuleAsync(Guid ruleId, CancellationToken cancellationToken = default);
    Task<Result<List<SoDRule>>> ListRulesAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<Result<List<SoDViolation>>> DetectViolationsAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);
    Task<Result<SoDViolation>> ResolveViolationAsync(Guid violationId, SoDResolutionAction action, string? notes, Guid resolvedBy, CancellationToken cancellationToken = default);
    Task<Result<List<SoDViolation>>> GetActiveViolationsAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<Result> ScanAllUsersAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<Result<SoDStatistics>> GetStatisticsAsync(Guid? tenantId, CancellationToken cancellationToken = default);
}

public class SoDStatistics
{
    public int TotalRules { get; set; }
    public int ActiveRules { get; set; }
    public int TotalViolations { get; set; }
    public int ActiveViolations { get; set; }
    public int ResolvedViolations { get; set; }
    public Dictionary<SoDSeverity, int> ViolationsBySeverity { get; set; } = new();
}
