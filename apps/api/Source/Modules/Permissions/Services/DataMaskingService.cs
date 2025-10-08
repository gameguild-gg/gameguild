using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Entities;
using GameGuild.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Permissions.Services;

/// <summary>
/// Service for managing and applying data masking rules
/// </summary>
public class DataMaskingService : IDataMaskingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataMaskingService> _logger;

    public DataMaskingService(
        ApplicationDbContext context,
        ILogger<DataMaskingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<MaskedFieldResult>> ApplyMaskingAsync(
        Guid userId,
        Guid? tenantId,
        string resourceType,
        string fieldName,
        object? fieldValue,
        string? resourceId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (fieldValue == null)
            {
                return Result<MaskedFieldResult>.Success(new MaskedFieldResult
                {
                    MaskedValue = null,
                    WasMasked = false
                });
            }

            // Get applicable masking rules
            var rules = await _context.Set<DataMaskingRule>()
                .Where(r => r.IsEnabled &&
                           r.DeletedAt == null &&
                           (r.TenantId == tenantId || r.TenantId == null) &&
                           r.ResourceType == resourceType &&
                           r.FieldName == fieldName)
                .OrderByDescending(r => r.Priority)
                .ToListAsync(cancellationToken);

            // If no rules, return original value
            if (!rules.Any())
            {
                return Result<MaskedFieldResult>.Success(new MaskedFieldResult
                {
                    MaskedValue = fieldValue,
                    WasMasked = false
                });
            }

            // Check if user is exempt from masking
            foreach (var rule in rules)
            {
                if (await IsUserExemptAsync(userId, rule, cancellationToken))
                {
                    await LogAccessAsync(userId, tenantId, resourceType, fieldName, resourceId,
                        rule.Id, false, "User exempt", cancellationToken);

                    return Result<MaskedFieldResult>.Success(new MaskedFieldResult
                    {
                        MaskedValue = fieldValue,
                        WasMasked = false,
                        RuleName = rule.Name,
                        RuleId = rule.Id,
                        Reason = "User exempt from masking"
                    });
                }

                // Apply the first applicable masking rule
                var maskedValue = ApplyMaskingRule(rule, fieldValue);

                if (rule.LogAccess)
                {
                    await LogAccessAsync(userId, tenantId, resourceType, fieldName, resourceId,
                        rule.Id, true, null, cancellationToken);
                }

                return Result<MaskedFieldResult>.Success(new MaskedFieldResult
                {
                    MaskedValue = maskedValue,
                    WasMasked = true,
                    RuleName = rule.Name,
                    RuleId = rule.Id
                });
            }

            // Default: return unmasked
            return Result<MaskedFieldResult>.Success(new MaskedFieldResult
            {
                MaskedValue = fieldValue,
                WasMasked = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying masking for {ResourceType}.{FieldName}", resourceType, fieldName);
            return Result<MaskedFieldResult>.Failure("Failed to apply data masking");
        }
    }

    public async Task<Result<Dictionary<string, object?>>> ApplyMaskingToObjectAsync<T>(
        Guid userId,
        Guid? tenantId,
        string resourceType,
        T obj,
        string? resourceId = null,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var result = new Dictionary<string, object?>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var fieldValue = property.GetValue(obj);
                var maskingResult = await ApplyMaskingAsync(
                    userId, tenantId, resourceType, property.Name, fieldValue, resourceId, cancellationToken);

                if (maskingResult.IsSuccess)
                {
                    result[property.Name] = maskingResult.Value!.MaskedValue;
                }
                else
                {
                    result[property.Name] = fieldValue;
                }
            }

            return Result<Dictionary<string, object?>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying masking to object of type {Type}", typeof(T).Name);
            return Result<Dictionary<string, object?>>.Failure("Failed to apply data masking to object");
        }
    }

    public async Task<Result<bool>> CanSeeUnmaskedAsync(
        Guid userId,
        Guid? tenantId,
        string resourceType,
        string fieldName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rules = await _context.Set<DataMaskingRule>()
                .Where(r => r.IsEnabled &&
                           r.DeletedAt == null &&
                           (r.TenantId == tenantId || r.TenantId == null) &&
                           r.ResourceType == resourceType &&
                           r.FieldName == fieldName)
                .ToListAsync(cancellationToken);

            if (!rules.Any())
                return Result<bool>.Success(true);

            foreach (var rule in rules)
            {
                if (!await IsUserExemptAsync(userId, rule, cancellationToken))
                    return Result<bool>.Success(false);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking unmasked access for {ResourceType}.{FieldName}",
                resourceType, fieldName);
            return Result<bool>.Failure("Failed to check masking permissions");
        }
    }

    public async Task<Result<DataMaskingRule>> CreateRuleAsync(
        DataMaskingRule rule,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rule.Name))
                return Result<DataMaskingRule>.Failure("Rule name is required");

            if (string.IsNullOrWhiteSpace(rule.ResourceType))
                return Result<DataMaskingRule>.Failure("Resource type is required");

            if (string.IsNullOrWhiteSpace(rule.FieldName))
                return Result<DataMaskingRule>.Failure("Field name is required");

            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;

            _context.Set<DataMaskingRule>().Add(rule);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created data masking rule {RuleId}: {RuleName}", rule.Id, rule.Name);
            return Result<DataMaskingRule>.Success(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating data masking rule");
            return Result<DataMaskingRule>.Failure("Failed to create data masking rule");
        }
    }

    public async Task<Result<DataMaskingRule>> UpdateRuleAsync(
        DataMaskingRule rule,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _context.Set<DataMaskingRule>()
                .FirstOrDefaultAsync(r => r.Id == rule.Id && r.DeletedAt == null, cancellationToken);

            if (existing == null)
                return Result<DataMaskingRule>.Failure("Rule not found");

            // Update properties
            existing.Name = rule.Name;
            existing.Description = rule.Description;
            existing.MaskingType = rule.MaskingType;
            existing.MaskingPattern = rule.MaskingPattern;
            existing.ShowFirst = rule.ShowFirst;
            existing.ShowLast = rule.ShowLast;
            existing.MaskCharacter = rule.MaskCharacter;
            existing.ExemptRoles = rule.ExemptRoles;
            existing.RequiredPermissions = rule.RequiredPermissions;
            existing.ExemptUsers = rule.ExemptUsers;
            existing.IsEnabled = rule.IsEnabled;
            existing.Priority = rule.Priority;
            existing.Conditions = rule.Conditions;
            existing.LogAccess = rule.LogAccess;
            existing.LogUnmaskedAccess = rule.LogUnmaskedAccess;
            existing.UpdatedBy = rule.UpdatedBy;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated data masking rule {RuleId}", rule.Id);
            return Result<DataMaskingRule>.Success(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating data masking rule {RuleId}", rule.Id);
            return Result<DataMaskingRule>.Failure("Failed to update data masking rule");
        }
    }

    public async Task<Result> DeleteRuleAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rule = await _context.Set<DataMaskingRule>()
                .FirstOrDefaultAsync(r => r.Id == ruleId && r.DeletedAt == null, cancellationToken);

            if (rule == null)
                return Result.Failure("Rule not found");

            rule.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted data masking rule {RuleId}", ruleId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting data masking rule {RuleId}", ruleId);
            return Result.Failure("Failed to delete data masking rule");
        }
    }

    public async Task<Result<DataMaskingRule>> GetRuleAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rule = await _context.Set<DataMaskingRule>()
                .FirstOrDefaultAsync(r => r.Id == ruleId && r.DeletedAt == null, cancellationToken);

            if (rule == null)
                return Result<DataMaskingRule>.Failure("Rule not found");

            return Result<DataMaskingRule>.Success(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data masking rule {RuleId}", ruleId);
            return Result<DataMaskingRule>.Failure("Failed to get data masking rule");
        }
    }

    public async Task<Result<List<DataMaskingRule>>> ListRulesAsync(
        Guid? tenantId,
        string? resourceType = null,
        bool includeDisabled = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Set<DataMaskingRule>()
                .Where(r => (r.TenantId == tenantId || r.TenantId == null) && r.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(resourceType))
                query = query.Where(r => r.ResourceType == resourceType);

            if (!includeDisabled)
                query = query.Where(r => r.IsEnabled);

            var rules = await query
                .OrderByDescending(r => r.Priority)
                .ThenBy(r => r.ResourceType)
                .ThenBy(r => r.FieldName)
                .ToListAsync(cancellationToken);

            return Result<List<DataMaskingRule>>.Success(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing data masking rules for tenant {TenantId}", tenantId);
            return Result<List<DataMaskingRule>>.Failure("Failed to list data masking rules");
        }
    }

    public async Task<Result<List<DataAccessLog>>> GetAccessLogsAsync(
        Guid? tenantId,
        Guid? userId = null,
        string? resourceType = null,
        bool? wasMasked = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Set<DataAccessLog>()
                .Where(l => l.TenantId == tenantId);

            if (userId.HasValue)
                query = query.Where(l => l.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(resourceType))
                query = query.Where(l => l.ResourceType == resourceType);

            if (wasMasked.HasValue)
                query = query.Where(l => l.WasMasked == wasMasked.Value);

            if (fromDate.HasValue)
                query = query.Where(l => l.AccessedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.AccessedAt <= toDate.Value);

            var logs = await query
                .OrderByDescending(l => l.AccessedAt)
                .Skip(skip)
                .Take(take)
                .Include(l => l.MaskingRule)
                .ToListAsync(cancellationToken);

            return Result<List<DataAccessLog>>.Success(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data access logs");
            return Result<List<DataAccessLog>>.Failure("Failed to get data access logs");
        }
    }

    public async Task<Result<MaskingStatistics>> GetStatisticsAsync(
        Guid? tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rules = await _context.Set<DataMaskingRule>()
                .Where(r => (r.TenantId == tenantId || r.TenantId == null) && r.DeletedAt == null)
                .ToListAsync(cancellationToken);

            var logsQuery = _context.Set<DataAccessLog>()
                .Where(l => l.TenantId == tenantId);

            if (fromDate.HasValue)
                logsQuery = logsQuery.Where(l => l.AccessedAt >= fromDate.Value);

            if (toDate.HasValue)
                logsQuery = logsQuery.Where(l => l.AccessedAt <= toDate.Value);

            var logs = await logsQuery.ToListAsync(cancellationToken);

            var stats = new MaskingStatistics
            {
                TotalRules = rules.Count,
                EnabledRules = rules.Count(r => r.IsEnabled),
                TotalAccesses = logs.Count,
                MaskedAccesses = logs.Count(l => l.WasMasked),
                UnmaskedAccesses = logs.Count(l => !l.WasMasked),
                AccessesByResourceType = logs.GroupBy(l => l.ResourceType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AccessesByField = logs.GroupBy(l => l.FieldName)
                    .ToDictionary(g => g.Key, g => g.Count()),
                RulesByMaskingType = rules.GroupBy(r => r.MaskingType)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Result<MaskingStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting masking statistics");
            return Result<MaskingStatistics>.Failure("Failed to get masking statistics");
        }
    }

    private async Task<bool> IsUserExemptAsync(
        Guid userId,
        DataMaskingRule rule,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check exempt users
            if (!string.IsNullOrWhiteSpace(rule.ExemptUsers))
            {
                var exemptUsers = JsonSerializer.Deserialize<List<Guid>>(rule.ExemptUsers);
                if (exemptUsers?.Contains(userId) == true)
                    return true;
            }

            // Check required permissions (simplified - can be enhanced)
            if (!string.IsNullOrWhiteSpace(rule.RequiredPermissions))
            {
                // This would need integration with permission service
                // For now, returning false (not exempt)
                return false;
            }

            // Check exempt roles (would need role service integration)
            if (!string.IsNullOrWhiteSpace(rule.ExemptRoles))
            {
                // This would need integration with role service
                return false;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private object? ApplyMaskingRule(DataMaskingRule rule, object? value)
    {
        if (value == null) return null;

        var stringValue = value.ToString() ?? string.Empty;

        return rule.MaskingType switch
        {
            MaskingType.FullMask => new string(rule.MaskCharacter, stringValue.Length),
            MaskingType.PartialMask => ApplyPartialMask(stringValue, rule),
            MaskingType.PatternMask => rule.MaskingPattern ?? stringValue,
            MaskingType.Hash => HashValue(stringValue),
            MaskingType.Encrypt => EncryptValue(stringValue),
            MaskingType.Nullify => null,
            MaskingType.Redact => "[REDACTED]",
            MaskingType.Tokenize => GenerateToken(stringValue),
            _ => stringValue
        };
    }

    private string ApplyPartialMask(string value, DataMaskingRule rule)
    {
        if (value.Length <= (rule.ShowFirst ?? 0) + (rule.ShowLast ?? 0))
            return value;

        var showFirst = rule.ShowFirst ?? 0;
        var showLast = rule.ShowLast ?? 0;
        var maskLength = value.Length - showFirst - showLast;

        var first = value.Substring(0, showFirst);
        var last = value.Substring(value.Length - showLast, showLast);
        var masked = new string(rule.MaskCharacter, maskLength);

        return first + masked + last;
    }

    private string HashValue(string value)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private string EncryptValue(string value)
    {
        // Simplified encryption - in production, use proper key management
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes);
    }

    private string GenerateToken(string value)
    {
        // Generate a consistent token for the same value
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = md5.ComputeHash(bytes);
        return $"TOK-{BitConverter.ToString(hash).Replace("-", "").Substring(0, 16)}";
    }

    private async Task LogAccessAsync(
        Guid userId,
        Guid? tenantId,
        string resourceType,
        string fieldName,
        string? resourceId,
        Guid? ruleId,
        bool wasMasked,
        string? unmaskedReason,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = new DataAccessLog
            {
                TenantId = tenantId,
                UserId = userId,
                MaskingRuleId = ruleId,
                ResourceType = resourceType,
                ResourceId = resourceId,
                FieldName = fieldName,
                WasMasked = wasMasked,
                UnmaskedReason = unmaskedReason,
                AccessedAt = DateTime.UtcNow
            };

            _context.Set<DataAccessLog>().Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log data access");
            // Don't throw - logging failure shouldn't break the operation
        }
    }
}
