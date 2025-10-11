using System.Reflection;
using System.Text.Json;


namespace GameGuild.Modules.Common.DataProtection;

/// <summary>
/// Service for scanning and reporting data classification metadata.
/// </summary>
public sealed class DataClassificationService
{
    private readonly ILogger<DataClassificationService> _logger;
    private readonly Dictionary<Type, List<PropertyClassification>> _cache = new();

    public DataClassificationService(ILogger<DataClassificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Scans an entity type for data classification attributes.
    /// </summary>
    public List<PropertyClassification> ScanEntity(Type entityType)
    {
        if (_cache.TryGetValue(entityType, out var cached))
        {
            return cached;
        }

        var classifications = new List<PropertyClassification>();

        foreach (var property in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = property.GetCustomAttribute<DataClassificationAttribute>();
            if (attr != null)
            {
                classifications.Add(new PropertyClassification
                {
                    PropertyName = property.Name,
                    PropertyType = property.PropertyType.Name,
                    Classification = attr.Classification,
                    LawfulBasis = attr.LawfulBasis,
                    RetentionDays = attr.RetentionDays,
                    RequiresEncryption = attr.RequiresEncryption,
                    MaskInLogs = attr.MaskInLogs,
                    ProcessingPurpose = attr.ProcessingPurpose,
                    IsErasable = property.GetCustomAttribute<GdprErasableAttribute>() != null,
                    IsPortable = property.GetCustomAttribute<GdprPortableAttribute>() != null,
                    IsExcluded = property.GetCustomAttribute<GdprExcludeAttribute>() != null
                });
            }
        }

        _cache[entityType] = classifications;
        return classifications;
    }

    /// <summary>
    /// Masks sensitive data in an object for logging.
    /// </summary>
    public object MaskForLogging(object obj)
    {
        if (obj == null) return null!;

        var type = obj.GetType();
        var classifications = ScanEntity(type);

        var masked = JsonSerializer.Deserialize<Dictionary<string, object>>(
            JsonSerializer.Serialize(obj)
        );

        if (masked == null) return obj;

        foreach (var classification in classifications.Where(c => c.MaskInLogs))
        {
            if (masked.ContainsKey(classification.PropertyName))
            {
                masked[classification.PropertyName] = "***REDACTED***";
            }
        }

        return masked;
    }

    /// <summary>
    /// Generates GDPR compliance report for entity types.
    /// </summary>
    public GdprComplianceReport GenerateComplianceReport(IEnumerable<Type> entityTypes)
    {
        var report = new GdprComplianceReport
        {
            GeneratedAt = DateTime.UtcNow,
            Entities = new List<EntityComplianceInfo>()
        };

        foreach (var entityType in entityTypes)
        {
            var classifications = ScanEntity(entityType);

            if (classifications.Any())
            {
                report.Entities.Add(new EntityComplianceInfo
                {
                    EntityName = entityType.Name,
                    FullName = entityType.FullName ?? entityType.Name,
                    Properties = classifications,
                    TotalPiiFields = classifications.Count(c => c.Classification.HasFlag(DataClassification.PII)),
                    TotalSensitiveFields = classifications.Count(c => c.Classification.HasFlag(DataClassification.SensitivePII)),
                    HasEncryptedFields = classifications.Any(c => c.RequiresEncryption),
                    HasRetentionPolicies = classifications.Any(c => c.RetentionDays.HasValue)
                });
            }
        }

        report.Summary = new ComplianceSummary
        {
            TotalEntities = report.Entities.Count,
            TotalPiiFields = report.Entities.Sum(e => e.TotalPiiFields),
            TotalSensitiveFields = report.Entities.Sum(e => e.TotalSensitiveFields),
            EntitiesWithEncryption = report.Entities.Count(e => e.HasEncryptedFields),
            EntitiesWithRetention = report.Entities.Count(e => e.HasRetentionPolicies)
        };

        return report;
    }

    /// <summary>
    /// Validates that all PII fields have proper classification.
    /// </summary>
    public ValidationResult ValidateClassifications(Type entityType)
    {
        var result = new ValidationResult { EntityType = entityType.Name };
        var classifications = ScanEntity(entityType);

        foreach (var classification in classifications)
        {
            // Check if PII has lawful basis
            if (classification.Classification.HasFlag(DataClassification.PII) &&
                !classification.LawfulBasis.HasValue)
            {
                result.Warnings.Add($"Property '{classification.PropertyName}' is PII but has no lawful basis specified");
            }

            // Check if sensitive PII requires encryption
            if (classification.Classification.HasFlag(DataClassification.SensitivePII) &&
                !classification.RequiresEncryption)
            {
                result.Warnings.Add($"Property '{classification.PropertyName}' is sensitive PII but encryption is not required");
            }

            // Check if PII has retention policy
            if (classification.Classification.HasFlag(DataClassification.PII) &&
                !classification.RetentionDays.HasValue)
            {
                result.Warnings.Add($"Property '{classification.PropertyName}' is PII but has no retention policy");
            }
        }

        result.IsValid = !result.Warnings.Any();
        return result;
    }
}

/// <summary>
/// Property classification metadata.
/// </summary>
public sealed class PropertyClassification
{
    public required string PropertyName { get; init; }
    public required string PropertyType { get; init; }
    public required DataClassification Classification { get; init; }
    public GdprLawfulBasis? LawfulBasis { get; init; }
    public int? RetentionDays { get; init; }
    public bool RequiresEncryption { get; init; }
    public bool MaskInLogs { get; init; }
    public string? ProcessingPurpose { get; init; }
    public bool IsErasable { get; init; }
    public bool IsPortable { get; init; }
    public bool IsExcluded { get; init; }
}

/// <summary>
/// GDPR compliance report.
/// </summary>
public sealed class GdprComplianceReport
{
    public DateTime GeneratedAt { get; set; }
    public List<EntityComplianceInfo> Entities { get; set; } = new();
    public ComplianceSummary Summary { get; set; } = new();
}

/// <summary>
/// Entity compliance information.
/// </summary>
public sealed class EntityComplianceInfo
{
    public required string EntityName { get; init; }
    public required string FullName { get; init; }
    public List<PropertyClassification> Properties { get; init; } = new();
    public int TotalPiiFields { get; init; }
    public int TotalSensitiveFields { get; init; }
    public bool HasEncryptedFields { get; init; }
    public bool HasRetentionPolicies { get; init; }
}

/// <summary>
/// Compliance summary statistics.
/// </summary>
public sealed class ComplianceSummary
{
    public int TotalEntities { get; set; }
    public int TotalPiiFields { get; set; }
    public int TotalSensitiveFields { get; set; }
    public int EntitiesWithEncryption { get; set; }
    public int EntitiesWithRetention { get; set; }
}

/// <summary>
/// Classification validation result.
/// </summary>
public sealed class ValidationResult
{
    public required string EntityType { get; init; }
    public bool IsValid { get; set; }
    public List<string> Warnings { get; init; } = new();
}
