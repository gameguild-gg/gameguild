namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validate tenant request
/// </summary>
public record ValidateTenantRequest(
    string Name,
    string Slug,
    string AdminEmail
);

/// <summary>
///     Tenant validation response
/// </summary>
public class TenantValidationResponse
{
    public bool IsValid { get; set; }
    public List<TenantValidationError> Errors { get; set; } = new();
    public List<TenantValidationWarning> Warnings { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public SlugValidation? SlugValidation { get; set; }
}

/// <summary>
///     Validation error detail
/// </summary>
public class TenantValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
///     Validation warning detail
/// </summary>
public class TenantValidationWarning
{
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
///     Slug validation result
/// </summary>
public class SlugValidation
{
    public bool IsAvailable { get; set; }
    public bool IsValid { get; set; }
    public List<string> SuggestedAlternatives { get; set; } = new();
}

/// <summary>
///     Tenant audit log entry
/// </summary>
public class TenantAuditLogEntry
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public string? ActorName { get; set; }
    public string? ActorEmail { get; set; }
    public Dictionary<string, object?> BeforeValues { get; set; } = new();
    public Dictionary<string, object?> AfterValues { get; set; } = new();
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
