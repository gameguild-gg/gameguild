namespace GameGuild.GraphQL;

/// <summary> GraphQL input for sharing a resource </summary>
public record ShareResourceInput(
    string ResourceType,
    Guid ResourceId,
    string[ ] UserEmails,
    Guid[ ] UserIds,
    PermissionType[ ] Permissions,
    DateTime? ExpiresAt,
    string? Message,
    bool RequireAcceptance = true,
    bool NotifyUsers = true
);
