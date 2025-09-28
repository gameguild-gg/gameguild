namespace GameGuild.GraphQL;

/// <summary> GraphQL input for updating user permissions </summary>
public record UpdateUserPermissionsInput(string ResourceType, Guid ResourceId, Guid TargetUserId, PermissionType[] Permissions, DateTime? ExpiresAt);