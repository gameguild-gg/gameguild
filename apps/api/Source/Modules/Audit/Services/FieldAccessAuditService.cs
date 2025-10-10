using System.Text.RegularExpressions;
using GameGuild.Modules.Audit.Entities;
using GameGuild.Modules.Audit.Enums;

namespace GameGuild.Modules.Audit.Services;

/// <summary>
/// Service for field-level data access auditing with PII masking capabilities
/// </summary>
public class FieldAccessAuditService : IFieldAccessAuditService
{
    private readonly IRepository<FieldAccessAudit, Guid> _repository;
    private readonly ILogger<FieldAccessAuditService> _logger;

    private static readonly Dictionary<string, SensitivityLevel> SensitiveFieldMap = new()
    {
        { "password", SensitivityLevel.HighlyRestricted },
        { "ssn", SensitivityLevel.HighlyRestricted },
        { "taxid", SensitivityLevel.HighlyRestricted },
        { "creditcard", SensitivityLevel.Restricted },
        { "bankaccount", SensitivityLevel.Restricted },
        { "email", SensitivityLevel.Confidential },
        { "phone", SensitivityLevel.Confidential },
        { "address", SensitivityLevel.Confidential },
        { "dateofbirth", SensitivityLevel.Confidential },
        { "salary", SensitivityLevel.Internal }
    };

    public FieldAccessAuditService(
        IRepository<FieldAccessAudit, Guid> repository,
        ILogger<FieldAccessAuditService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<FieldAccessAudit> RecordFieldAccessAsync(
        Guid? tenantId,
        Guid userId,
        string entityType,
        string entityId,
        string fieldName,
        FieldAccessType accessType,
        string? oldValue,
        string? newValue,
        string? ipAddress,
        string? userAgent,
        string? requestId,
        string? sessionId,
        string? apiEndpoint,
        string? legalBasis,
        Guid? consentId,
        CancellationToken cancellationToken = default)
    {
        var sensitivityLevel = DetermineSensitivityLevel(fieldName);
        var isSensitive = sensitivityLevel >= SensitivityLevel.Confidential;

        var audit = FieldAccessAudit.Create(
            tenantId,
            userId,
            entityType,
            entityId,
            fieldName,
            accessType);

        // Set values with masking for sensitive fields
        var maskedOldValue = isSensitive ? MaskSensitiveData(oldValue, sensitivityLevel) : oldValue;
        var maskedNewValue = isSensitive ? MaskSensitiveData(newValue, sensitivityLevel) : newValue;

        audit.SetValues(oldValue, newValue, maskedOldValue, maskedNewValue, isSensitive, sensitivityLevel);
        audit.SetAccessContext(ipAddress, userAgent, requestId, sessionId, apiEndpoint);

        // Determine if notification is required (e.g., for GDPR)
        var requiresNotification = isSensitive &&
            (accessType == FieldAccessType.Export || accessType == FieldAccessType.Delete);

        if (!string.IsNullOrEmpty(legalBasis) || consentId.HasValue)
        {
            audit.SetComplianceInfo(legalBasis, consentId, requiresNotification);
        }

        await _repository.AddAsync(audit, cancellationToken);

        _logger.LogInformation(
            "Recorded field access: {EntityType}.{FieldName} by user {UserId} - {AccessType}",
            entityType,
            fieldName,
            userId,
            accessType);

        return audit;
    }

    public string MaskSensitiveData(string? data, SensitivityLevel sensitivityLevel)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;

        return sensitivityLevel switch
        {
            SensitivityLevel.HighlyRestricted => "***REDACTED***",
            SensitivityLevel.Restricted => MaskWithPattern(data, 0.9), // Show 10%
            SensitivityLevel.Confidential => MaskWithPattern(data, 0.7), // Show 30%
            SensitivityLevel.Internal => MaskWithPattern(data, 0.5), // Show 50%
            _ => data
        };
    }

    public string RedactPii(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Redact email addresses
        text = Regex.Replace(text, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", "[EMAIL]");

        // Redact phone numbers (various formats)
        text = Regex.Replace(text, @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", "[PHONE]");
        text = Regex.Replace(text, @"\b\+?\d{1,3}[-.\s]?\(?\d{1,4}\)?[-.\s]?\d{1,4}[-.\s]?\d{1,9}\b", "[PHONE]");

        // Redact SSN patterns
        text = Regex.Replace(text, @"\b\d{3}-\d{2}-\d{4}\b", "[SSN]");

        // Redact credit card numbers
        text = Regex.Replace(text, @"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", "[CREDITCARD]");

        // Redact IP addresses
        text = Regex.Replace(text, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b", "[IPADDRESS]");

        return text;
    }

    public async Task<List<FieldAccessAudit>> GetFieldAccessHistoryAsync(
        string entityType,
        string entityId,
        string? fieldName = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _repository.AsQueryable()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId);

        if (!string.IsNullOrEmpty(fieldName))
            query = query.Where(x => x.FieldName == fieldName);

        return await query
            .OrderByDescending(x => x.AccessedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FieldAccessAudit>> GetSensitiveFieldAccessesAsync(
        Guid? tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        SensitivityLevel? minSensitivityLevel = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _repository.AsQueryable()
            .Where(x => x.IsSensitiveField);

        if (tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId);

        if (startDate.HasValue)
            query = query.Where(x => x.AccessedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(x => x.AccessedAt <= endDate.Value);

        if (minSensitivityLevel.HasValue)
            query = query.Where(x => x.SensitivityLevel >= minSensitivityLevel.Value);

        return await query
            .OrderByDescending(x => x.AccessedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    private SensitivityLevel DetermineSensitivityLevel(string fieldName)
    {
        var normalizedName = fieldName.ToLowerInvariant().Replace("_", "").Replace("-", "");

        foreach (var kvp in SensitiveFieldMap)
        {
            if (normalizedName.Contains(kvp.Key))
                return kvp.Value;
        }

        return SensitivityLevel.Public;
    }

    private string MaskWithPattern(string data, double maskPercentage)
    {
        if (data.Length <= 4)
            return new string('*', data.Length);

        var charsToMask = (int)(data.Length * maskPercentage);
        var charsToShow = data.Length - charsToMask;
        var showAtStart = charsToShow / 2;
        var showAtEnd = charsToShow - showAtStart;

        var masked = data.Substring(0, showAtStart) +
                     new string('*', charsToMask) +
                     data.Substring(data.Length - showAtEnd);

        return masked;
    }
}
