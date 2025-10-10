using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Permissions.Services;

public class SoDService : ISoDService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SoDService> _logger;

    public SoDService(ApplicationDbContext context, ILogger<SoDService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<SoDRule>> CreateRuleAsync(SoDRule rule, CancellationToken cancellationToken = default)
    {
        try
        {
            rule.CreatedAt = DateTime.UtcNow;
            _context.Set<SoDRule>().Add(rule);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created SoD rule {RuleId} '{RuleName}'", rule.Id, rule.Name);
            return Result<SoDRule>.Success(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SoD rule");
            return Result<SoDRule>.Failure($"Failed to create rule: {ex.Message}");
        }
    }

    public async Task<Result<SoDRule>> UpdateRuleAsync(SoDRule rule, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _context.Set<SoDRule>().FirstOrDefaultAsync(r => r.Id == rule.Id, cancellationToken);
            if (existing == null)
                return Result<SoDRule>.Failure("Rule not found");

            existing.Name = rule.Name;
            existing.Description = rule.Description;
            existing.RuleType = rule.RuleType;
            existing.Severity = rule.Severity;
            existing.IsEnabled = rule.IsEnabled;
            existing.ConflictingPermissions = rule.ConflictingPermissions;
            existing.ConflictingRoles = rule.ConflictingRoles;
            existing.ConflictingResources = rule.ConflictingResources;
            existing.AllowedExceptions = rule.AllowedExceptions;
            existing.RequireApproval = rule.RequireApproval;
            existing.ApproverRoles = rule.ApproverRoles;
            existing.MitigationStrategy = rule.MitigationStrategy;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated SoD rule {RuleId}", rule.Id);
            return Result<SoDRule>.Success(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update SoD rule {RuleId}", rule.Id);
            return Result<SoDRule>.Failure($"Failed to update rule: {ex.Message}");
        }
    }

    public async Task<Result> DeleteRuleAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rule = await _context.Set<SoDRule>().FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken);
            if (rule == null)
                return Result.Failure("Rule not found");

            _context.Set<SoDRule>().Remove(rule);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted SoD rule {RuleId}", ruleId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete SoD rule {RuleId}", ruleId);
            return Result.Failure($"Failed to delete rule: {ex.Message}");
        }
    }

    public async Task<Result<SoDRule>> GetRuleAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rule = await _context.Set<SoDRule>()
                .Include(r => r.Violations)
                .FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken);

            if (rule == null)
                return Result<SoDRule>.Failure("Rule not found");

            return Result<SoDRule>.Success(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SoD rule {RuleId}", ruleId);
            return Result<SoDRule>.Failure($"Failed to get rule: {ex.Message}");
        }
    }

    public async Task<Result<List<SoDRule>>> ListRulesAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Set<SoDRule>().AsQueryable();
            if (tenantId.HasValue)
                query = query.Where(r => r.TenantId == tenantId.Value);

            var rules = await query.OrderBy(r => r.Name).ToListAsync(cancellationToken);
            return Result<List<SoDRule>>.Success(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list SoD rules");
            return Result<List<SoDRule>>.Failure($"Failed to list rules: {ex.Message}");
        }
    }

    public async Task<Result<List<SoDViolation>>> DetectViolationsAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rules = await _context.Set<SoDRule>()
                .Where(r => r.IsEnabled && (!tenantId.HasValue || r.TenantId == tenantId.Value))
                .ToListAsync(cancellationToken);

            var violations = new List<SoDViolation>();
            foreach (var rule in rules)
            {
                // Simple conflict detection logic (would be more sophisticated in production)
                var violation = new SoDViolation
                {
                    RuleId = rule.Id,
                    UserId = userId,
                    TenantId = tenantId,
                    Status = SoDViolationStatus.Active,
                    ViolationDetails = $"User violates rule: {rule.Name}",
                    ConflictingItems = rule.ConflictingPermissions,
                    DetectedAt = DateTime.UtcNow
                };
                _context.Set<SoDViolation>().Add(violation);
                violations.Add(violation);

                rule.ViolationCount++;
                rule.LastViolationDetected = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Detected {Count} violations for user {UserId}", violations.Count, userId);
            return Result<List<SoDViolation>>.Success(violations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect violations for user {UserId}", userId);
            return Result<List<SoDViolation>>.Failure($"Failed to detect violations: {ex.Message}");
        }
    }

    public async Task<Result<SoDViolation>> ResolveViolationAsync(
        Guid violationId,
        SoDResolutionAction action,
        string? notes,
        Guid resolvedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var violation = await _context.Set<SoDViolation>().FirstOrDefaultAsync(v => v.Id == violationId, cancellationToken);
            if (violation == null)
                return Result<SoDViolation>.Failure("Violation not found");

            violation.Status = SoDViolationStatus.Resolved;
            violation.ResolutionAction = action;
            violation.ResolutionNotes = notes;
            violation.ResolvedBy = resolvedBy;
            violation.ResolvedAt = DateTime.UtcNow;
            violation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Resolved violation {ViolationId} with action {Action}", violationId, action);
            return Result<SoDViolation>.Success(violation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve violation {ViolationId}", violationId);
            return Result<SoDViolation>.Failure($"Failed to resolve violation: {ex.Message}");
        }
    }

    public async Task<Result<List<SoDViolation>>> GetActiveViolationsAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Set<SoDViolation>()
                .Include(v => v.Rule)
                .Where(v => v.Status == SoDViolationStatus.Active);

            if (tenantId.HasValue)
                query = query.Where(v => v.TenantId == tenantId.Value);

            var violations = await query.OrderByDescending(v => v.DetectedAt).ToListAsync(cancellationToken);
            return Result<List<SoDViolation>>.Success(violations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active violations");
            return Result<List<SoDViolation>>.Failure($"Failed to get violations: {ex.Message}");
        }
    }

    public async Task<Result> ScanAllUsersAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simplified: In production, this would iterate all users and check for conflicts
            _logger.LogInformation("Starting SoD scan for all users in tenant {TenantId}", tenantId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan all users");
            return Result.Failure($"Failed to scan users: {ex.Message}");
        }
    }

    public async Task<Result<SoDStatistics>> GetStatisticsAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rulesQuery = _context.Set<SoDRule>().AsQueryable();
            var violationsQuery = _context.Set<SoDViolation>().AsQueryable();

            if (tenantId.HasValue)
            {
                rulesQuery = rulesQuery.Where(r => r.TenantId == tenantId.Value);
                violationsQuery = violationsQuery.Where(v => v.TenantId == tenantId.Value);
            }

            var stats = new SoDStatistics
            {
                TotalRules = await rulesQuery.CountAsync(cancellationToken),
                ActiveRules = await rulesQuery.CountAsync(r => r.IsEnabled, cancellationToken),
                TotalViolations = await violationsQuery.CountAsync(cancellationToken),
                ActiveViolations = await violationsQuery.CountAsync(v => v.Status == SoDViolationStatus.Active, cancellationToken),
                ResolvedViolations = await violationsQuery.CountAsync(v => v.Status == SoDViolationStatus.Resolved, cancellationToken)
            };

            return Result<SoDStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SoD statistics");
            return Result<SoDStatistics>.Failure($"Failed to get statistics: {ex.Message}");
        }
    }
}
