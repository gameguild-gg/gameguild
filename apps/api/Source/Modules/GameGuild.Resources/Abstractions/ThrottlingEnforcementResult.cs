namespace GameGuild.Resources;

public sealed record ThrottlingEnforcementResult(
    Guid TenantId,
    ResourceUsageType ResourceType,
    bool IsEnforced,
    string EnforcementReference,
    DateTime EnforcedAt,
    int RetryAfterMs,
    string? Reason);
